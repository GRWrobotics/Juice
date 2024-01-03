using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Created by Glen Warren, 12/23 - 1/2/24
//This script generates a random level from a set of pre-built prefab "Chunks"
//The level is generated completely on script start and will require performance adjustement in the future
//There is a chance to randomly transition into or out of vertical level sections


public class LevelGenerator : MonoBehaviour
{
    //The level start chunk
    [SerializeField] GameObject startChunk;
    //The Level end chunk
    [SerializeField] GameObject endChunk;
    //Level Chunks for horizontal sections
    [SerializeField] GameObject[] horizontalChunks;
    //Level Chunks for vertical sections
    [SerializeField] GameObject[] verticalChunks;
    //Level Chunks that are used to transition to vertical sections
    [SerializeField] GameObject[] verticalTransitions;
    //Level Chunks that are used to transfer into horizontal sections
    [SerializeField] GameObject[] horizontalTransitions;
    //The total length of the level in chunks
    [SerializeField] int levelLength;
    //The transform of the first level chunk
    [SerializeField] Transform levelStartSpawn;

    Vector3 spawnPoint;
    float levelSizeX = 20;
    float levelSizeY = 11;
    Vector3 horizontalLevelOffset; 
    Vector3 verticalLevelOffset; 
    //Used to randomly select a chunk from their arrays
    int chunkIndex;
    //Used to randomly transition between horiz and vert sections
    int randomInt;
    bool verticalSpawning = false;
    GameObject chunk;
    [SerializeField] int enterVerticalChance = 10;
    [SerializeField] int exitVerticalChance = 25;
    void Start()
    {
        //Start the spawn point at the levelStart and spawn the start
        spawnPoint = levelStartSpawn.transform.position;
        Instantiate(startChunk, levelStartSpawn);
        
        //Initialize Vector 3 offsets based on chunk size for how far horizontal and vertical chunks will be offset
        horizontalLevelOffset = new Vector3(levelSizeX, 0, 0);
        verticalLevelOffset = new Vector3(0, levelSizeY, 0);

        for(int i = 0; i < levelLength; i++){
            randomInt = UnityEngine.Random.Range(0, 100);
            //Move the chunk spawn point
            if(!verticalSpawning){
                spawnPoint += horizontalLevelOffset;
            }
            else{
                spawnPoint += verticalLevelOffset;
            }
            
            //Based on random chance, insert a transition into or out of verticality. 
            if(!verticalSpawning && randomInt <= enterVerticalChance){
                verticalSpawning = true;
                chunkIndex = UnityEngine.Random.Range(0, verticalTransitions.Length);
                chunk = Instantiate(verticalTransitions[chunkIndex], levelStartSpawn);
            }
            else if(verticalSpawning && randomInt <= exitVerticalChance){
                verticalSpawning = false;
                chunkIndex = UnityEngine.Random.Range(0, horizontalTransitions.Length);
                Debug.Log(chunkIndex);
                chunk = Instantiate(horizontalTransitions[chunkIndex], levelStartSpawn);
            }
            //Otherwise, insert a regular vertical or horizontal chunk
            else{
                if(!verticalSpawning){
                    chunkIndex = UnityEngine.Random.Range(0, horizontalChunks.Length);
                    chunk = Instantiate(horizontalChunks[chunkIndex], levelStartSpawn);
                }else{
                    chunkIndex = UnityEngine.Random.Range(0, verticalChunks.Length);
                    chunk = Instantiate(verticalChunks[chunkIndex], levelStartSpawn);
                }
            }
            //After instantiation, move the chunk to the correct position.
            chunk.transform.position = spawnPoint;
        }
        //Insert the end chunk (we may not have one)
        spawnPoint += horizontalLevelOffset;
        GameObject endChunkObj = Instantiate(endChunk, levelStartSpawn);
        endChunkObj.transform.position = spawnPoint;    
    }
    // Update is called once per frame
    // We could conduct spawning and removing of level chunks here 
    void Update()
    {

    }
}
