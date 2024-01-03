using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using EZCameraShake;
using System.Collections;

namespace TarodevController {
    /// <summary>
    /// Hey!
    /// Tarodev here. I built this controller as there was a severe lack of quality & free 2D controllers out there.
    /// Right now it only contains movement and jumping, but it should be pretty easy to expand... I may even do it myself
    /// if there's enough interest. You can play and compete for best times here: https://tarodev.itch.io/
    /// If you hve any questions or would like to brag about your score, come to discord: https://discord.gg/GqeHHnhHpz
    /// </summary>
    /// 
    //Edited by Glen Warren 12/19/23 for wall jumping, turning, shooting, and strafing. 
    public class PlayerController : MonoBehaviour, IPlayerController {

        // Public for external hooks
        public Vector3 Velocity { get; private set; }
        public FrameInput Input { get; private set; }
        public bool JumpingThisFrame { get; private set; }
        public bool LandingThisFrame { get; private set; }
        public Vector3 RawMovement { get; private set; }
        public bool Grounded => _colDown;

        private Vector3 _lastPosition;
        private float _currentHorizontalSpeed, _currentVerticalSpeed;

        // This is horrible, but for some reason colliders are not fully established when update starts...
        private bool _active;
        void Awake() => Invoke(nameof(Activate), 0.5f);
        void Activate() =>  _active = true;
        
        private void Update() {
            if(!_active) return;
            // Calculate velocity
            Velocity = (transform.position - _lastPosition) / Time.deltaTime;
            _lastPosition = transform.position;

            GatherInput();
            RunCollisionChecks();

            CalculateWalk(); // Horizontal movement
            CalculateJumpApex(); // Affects fall speed, so calculate before gravity
            CalculateGravity(); // Vertical movement
            CalculateJump(); // Possibly overrides vertical

            MoveCharacter(); // Actually perform the axis movement
            Shoot();
            StartCoroutine(LerpObject(gun.transform, gunHome.transform, gunSmoothing));
        }


        #region Gather Input

        private void GatherInput() {
            Input = new FrameInput {
                JumpDown = UnityEngine.Input.GetButtonDown("Jump"),
                JumpUp = UnityEngine.Input.GetButtonUp("Jump"),
                X = UnityEngine.Input.GetAxisRaw("Horizontal"),
                Fire1Down = UnityEngine.Input.GetButtonDown("Fire1"),
                Fire1Up = UnityEngine.Input.GetButtonUp("Fire1")
            };
            if (Input.JumpDown) {
                _lastJumpPressed = Time.time;
            }
        }

        #endregion

        #region Collisions

        [Header("COLLISION")] [SerializeField] private Bounds _characterBounds;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private int _detectorCount = 3;
        [SerializeField] private float _detectionRayLength = 0.1f;
        [SerializeField] [Range(0.1f, 0.3f)] private float _rayBuffer = 0.1f; // Prevents side detectors hitting the ground

        private RayRange _raysUp, _raysRight, _raysDown, _raysLeft;
        private bool _colUp, _colRight, _colDown, _colLeft;

        private float _timeLeftGrounded;

        // We use these raycast checks for pre-collision information
        private void RunCollisionChecks() {
            // Generate ray ranges. 
            CalculateRayRanged();

            // Ground
            LandingThisFrame = false;
            var groundedCheck = RunDetection(_raysDown);
            var walledCheck = RunDetection(_raysLeft)||RunDetection(_raysRight);
            if (_colDown && !groundedCheck) _timeLeftGrounded = Time.time; // Only trigger when first leaving
            else if (!_colDown && groundedCheck) {
                _coyoteUsable = true; // Only trigger when first touching
                LandingThisFrame = true;
            }

            _colDown = groundedCheck;

            // The rest
            _colUp = RunDetection(_raysUp);
            _colLeft = RunDetection(_raysLeft);
            _colRight = RunDetection(_raysRight);

            bool RunDetection(RayRange range) {
                return EvaluateRayPositions(range).Any(point => Physics2D.Raycast(point, range.Dir, _detectionRayLength, _groundLayer));
            }
        }

        private void CalculateRayRanged() {
            // This is crying out for some kind of refactor. 
            var b = new Bounds(transform.position, _characterBounds.size);

            _raysDown = new RayRange(b.min.x + _rayBuffer, b.min.y, b.max.x - _rayBuffer, b.min.y, Vector2.down);
            _raysUp = new RayRange(b.min.x + _rayBuffer, b.max.y, b.max.x - _rayBuffer, b.max.y, Vector2.up);
            _raysLeft = new RayRange(b.min.x, b.min.y + _rayBuffer, b.min.x, b.max.y - _rayBuffer, Vector2.left);
            _raysRight = new RayRange(b.max.x, b.min.y + _rayBuffer, b.max.x, b.max.y - _rayBuffer, Vector2.right);
        }


        private IEnumerable<Vector2> EvaluateRayPositions(RayRange range) {
            for (var i = 0; i < _detectorCount; i++) {
                var t = (float)i / (_detectorCount - 1);
                yield return Vector2.Lerp(range.Start, range.End, t);
            }
        }

        private void OnDrawGizmos() {
            // Bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + _characterBounds.center, _characterBounds.size);

            // Rays
            if (!Application.isPlaying) {
                CalculateRayRanged();
                Gizmos.color = Color.blue;
                foreach (var range in new List<RayRange> { _raysUp, _raysRight, _raysDown, _raysLeft }) {
                    foreach (var point in EvaluateRayPositions(range)) {
                        Gizmos.DrawRay(point, range.Dir * _detectionRayLength);
                    }
                }
            }

            if (!Application.isPlaying) return;

            // Draw the future position. Handy for visualizing gravity
            Gizmos.color = Color.red;
            var move = new Vector3(_currentHorizontalSpeed, _currentVerticalSpeed) * Time.deltaTime;
            Gizmos.DrawWireCube(transform.position + move, _characterBounds.size);
        }

        #endregion


        #region Walk

        [Header("WALKING")] [SerializeField] private float _acceleration = 90;
        [SerializeField] private float _moveClamp = 13;
        [SerializeField] private float _deAcceleration = 60f;
        [SerializeField] private float _apexBonus = 2;
        private Transform originalScale;
        private bool facingRight = true;
        
        private void CalculateWalk() {
            if (Input.X != 0) {
                // Set horizontal move speed
                _currentHorizontalSpeed += Input.X * _acceleration * Time.deltaTime;

                // clamped by max frame movement
                _currentHorizontalSpeed = Mathf.Clamp(_currentHorizontalSpeed, -_moveClamp, _moveClamp);

                // Apply bonus at the apex of a jump
                var apexBonus = Mathf.Sign(Input.X) * _apexBonus * _apexPoint;
                _currentHorizontalSpeed += apexBonus * Time.deltaTime;
            }
            else {
                // No input. Let's slow the character down
                _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, 0, _deAcceleration * Time.deltaTime);
            }

            if (_currentHorizontalSpeed > 0 && _colRight || _currentHorizontalSpeed < 0 && _colLeft) {
                // Don't walk through walls
                _currentHorizontalSpeed = 0;
            }
            //Look where you're walking + strafe if firing
            //Want this to be based on the charachter's scaling but can't figure it out so nums are hardcoded
            if(!firing && Input.X > 0){
                transform.localScale = new Vector3(0.5f, 0.5f, 1);
                facingRight = true;
            }else if(!firing && Input.X < 0){
                transform.localScale = new Vector3(-0.5f, 0.5f, 1);
                facingRight = false;
            }
        }

        #endregion

        #region Gravity

        [Header("GRAVITY")] [SerializeField] private float _fallClamp = -40f;
        [SerializeField] private float _minFallSpeed = 80f;
        [SerializeField] private float _maxFallSpeed = 120f;
        [SerializeField] private float _maxSlideSpeed = -10f;
        private float _fallSpeed;

        private void CalculateGravity() {
            if (_colDown) {
                // Move out of the ground
                if (_currentVerticalSpeed < 0) _currentVerticalSpeed = 0;
            }
            else if((_colLeft||_colRight) && (_currentVerticalSpeed < _maxSlideSpeed)){
                _currentVerticalSpeed = _maxSlideSpeed;
            }
            else {
                // Add downward force while ascending if we ended the jump early
                var fallSpeed = _endedJumpEarly && _currentVerticalSpeed > 0 ? _fallSpeed * _jumpEndEarlyGravityModifier : _fallSpeed;

                // Fall
                _currentVerticalSpeed -= fallSpeed * Time.deltaTime;

                // Clamp
                if (_currentVerticalSpeed < _fallClamp) _currentVerticalSpeed = _fallClamp;
            }
        }

        #endregion

        #region Jump

        [Header("JUMPING")] [SerializeField] private float _jumpHeight = 30;
        [SerializeField] private float _jumpApexThreshold = 10f;
        [SerializeField] private float _coyoteTimeThreshold = 0.1f;
        [SerializeField] private float _jumpBuffer = 0.1f;
        [SerializeField] private float _jumpEndEarlyGravityModifier = 3;
        [SerializeField] private float _wallJumpDelay = 0.1f;
        private bool _coyoteUsable;
        private bool _endedJumpEarly = true;
        private float _apexPoint; // Becomes 1 at the apex of a jump
        private float _lastJumpPressed;
        private bool CanUseCoyote => _coyoteUsable && !_colDown && _timeLeftGrounded + _coyoteTimeThreshold > Time.time;
        private bool HasBufferedJump => _colDown && _lastJumpPressed + _jumpBuffer > Time.time;
        bool CanWallJump => _colLeft || _colRight; // && ((_lastJumpPressed + _wallJumpDelay) < Time.time);
        
        private void CalculateJumpApex() {
            if (!_colDown) {
                // Gets stronger the closer to the top of the jump
                _apexPoint = Mathf.InverseLerp(_jumpApexThreshold, 0, Mathf.Abs(Velocity.y));
                _fallSpeed = Mathf.Lerp(_minFallSpeed, _maxFallSpeed, _apexPoint);
            }
            else {
                _apexPoint = 0;
            }
        }

        private void CalculateJump() {
            // Jump if: grounded or within coyote threshold || sufficient jump buffer
            if (Input.JumpDown && (CanUseCoyote || CanWallJump) || HasBufferedJump) {
                _currentVerticalSpeed = _jumpHeight;
                _endedJumpEarly = false;
                _coyoteUsable = false;
                _timeLeftGrounded = float.MinValue;
                JumpingThisFrame = true;
            }
            else {
                JumpingThisFrame = false;
            }

            // End the jump early if button released
            if (!_colDown && Input.JumpUp && !_endedJumpEarly && Velocity.y > 0) {
                // _currentVerticalSpeed = 0;
                _endedJumpEarly = true;
            }

            if (_colUp) {
                if (_currentVerticalSpeed > 0) _currentVerticalSpeed = 0;
            }
        }

        #endregion

        #region Move

        [Header("MOVE")] [SerializeField, Tooltip("Raising this value increases collision accuracy at the cost of performance.")]
        private int _freeColliderIterations = 10;

        // We cast our bounds before moving to avoid future collisions
        private void MoveCharacter() {
            var pos = transform.position;
            RawMovement = new Vector3(_currentHorizontalSpeed, _currentVerticalSpeed); // Used externally
            var move = RawMovement * Time.deltaTime;
            var furthestPoint = pos + move;

            // check furthest movement. If nothing hit, move and don't do extra checks
            var hit = Physics2D.OverlapBox(furthestPoint, _characterBounds.size, 0, _groundLayer);
            if (!hit) {
                transform.position += move;
                return;
            }

            // otherwise increment away from current pos; see what closest position we can move to
            var positionToMoveTo = transform.position;
            for (int i = 1; i < _freeColliderIterations; i++) {
                // increment to check all but furthestPoint - we did that already
                var t = (float)i / _freeColliderIterations;
                var posToTry = Vector2.Lerp(pos, furthestPoint, t);

                if (Physics2D.OverlapBox(posToTry, _characterBounds.size, 0, _groundLayer)) {
                    transform.position = positionToMoveTo;

                    // We've landed on a corner or hit our head on a ledge. Nudge the player gently
                    if (i == 1) {
                        if (_currentVerticalSpeed < 0) _currentVerticalSpeed = 0;
                        var dir = transform.position - hit.transform.position;
                        transform.position += dir.normalized * move.magnitude;
                    }
                    return;
                }

                positionToMoveTo = posToTry;
            }
        }

        #endregion
        #region Shoot
        [Header("SHOOT")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] GameObject projectile;
        [SerializeField] private float fireRate = 0.05f;
        [SerializeField] private float shootForce = 10f;
        [SerializeField] private float spreadForce = 2f;
        [SerializeField] private int numBullets = 1;
        [SerializeField] private Camera camera;
        [SerializeField] private float shakeForce = 1f;
        [SerializeField] private float shakeRoughness = 1f;
        [SerializeField] private float shakeInTime = 0.1f;
        [SerializeField] private float shakeOutTime = 0.1f;
        [SerializeField] private GameObject gun;
        [SerializeField] private GameObject gunHome;
        [SerializeField] private float gunKickMag;
        [SerializeField] private float gunSmoothing = 0.1f;
        [SerializeField] private float knockbackForce = 0.1f;
        private float fireTimer = 0;
        private bool firing = false;
        private Vector2 fireVelocity;
        private Vector3 knockbackDirection;
        void Shoot(){
            if(Input.Fire1Down){
                firing = true;
            }else if(Input.Fire1Up){
                firing = false;
            }
            if(firing){
                if(fireTimer>fireRate){
                    for(int i = 0; i < numBullets; i++){
                        GameObject bullet = Instantiate(projectile, spawnPoint.position, spawnPoint.rotation);
                        Rigidbody2D bulletRB = bullet.GetComponent<Rigidbody2D>();
                        if(facingRight){
                            fireVelocity = new Vector2(1,0) * shootForce + 
                            new Vector2(0,1) * UnityEngine.Random.Range(-spreadForce, spreadForce);
                            knockbackDirection = new Vector3(-1, 0, 0);
                        }else{
                            fireVelocity = new Vector2(-1,0) * shootForce + 
                            new Vector2(0,1) * UnityEngine.Random.Range(-spreadForce, spreadForce);
                            knockbackDirection = new Vector3(1, 0, 0);
                        }
                        
                        bulletRB.velocity = fireVelocity;
                    }
                    
                    CameraShaker.Instance.ShakeOnce(shakeForce, shakeRoughness, shakeInTime, shakeOutTime);
                    // StartCoroutine(Shake(gun, fireRate, gunShakeMag));
                    if(!_colRight && !_colLeft){
                        Knockback(gameObject, knockbackDirection, knockbackForce);
                    }
                    Knockback(gun, knockbackDirection, gunKickMag);
                    fireTimer = 0;
                }
                fireTimer+=Time.deltaTime;
            }
        }
        private IEnumerator Shake(GameObject obj, float duration, float magnitude){
            Vector3 originalPos = obj.transform.localPosition;
            float shakeTimer = 0;
            
            while(shakeTimer < duration){
                float x = obj.transform.localPosition.x + UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = obj.transform.localPosition.y + UnityEngine.Random.Range(-1f, 1f) * magnitude;
                
                obj.transform.localPosition = new Vector3(x, y, originalPos.z); 
                
                shakeTimer+=Time.deltaTime;
                
                yield return null;
            }
            obj.transform.localPosition = originalPos;   
        }
        private void Knockback(GameObject obj, Vector3 direction, float magnitude){
            obj.transform.position += direction * magnitude;
        }
        IEnumerator LerpObject(Transform obj, Transform target, float smoothing){
            Vector3 objPos = obj.position;
            Vector3 targetPos = target.position;
            obj.position = Vector3.Lerp(objPos, targetPos, smoothing);
            yield return null;
        }

        #endregion
    }
}