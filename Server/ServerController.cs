using SnakeGame;
using SnakeGame.TheMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
/**
* University of Utah Course: CS 3500-001 Fall 2023 Software Practice
*
* Semester: Fall 2023
*
* Assignment: 9
*
* @Author: Duy Khanh Tran & Orion Harmon
*
* Created date: 12/07/2023
*
* Description: Represents the game ServerController
 */
namespace Server
{
    internal class ServerController
    {
        private World theWorld;
        public GameSettings setting { get; set; }
        /// <summary>
        /// Initializes a new instance of the ServerController class
        /// </summary>
        public ServerController()
        {
            setting = new();
            // Create a new game world with a specified size
            theWorld = new World(setting.universeSize);
        }

        /// <summary>
        /// Builds the game world by adding walls and generating hitboxes for walls
        /// </summary>
        /// <param name="wallList">The list of walls to add to the game world</param>
        public void BuildWorld(List<Wall> wallList)
        {
            // Add each wall from the provided list to the game world's WallsList
            foreach (Wall wall in wallList)
            {
                theWorld.WallsList[wall.wall] = wall;
            }

            // Generate hitboxes for the walls in the game world
            theWorld.buildHitboxesWalls();
            theWorld.spawnFood();
        }


        /// <summary>
        /// Updates the state of the game world
        /// </summary>
        public void UpdateWorld()
        {
            // Respawn food items in the game world
            lock (theWorld)
                foreach (Powerup powerup in theWorld.PowerupsList.Values)
                    if (powerup.died)
                        RespawnFood(powerup);

            // Lock the game world 
            lock (theWorld)
            {
                // Iterate through each snake in the world

                foreach (int snakeID in theWorld.SnakesList.Keys)
                {
                    Snake snake = theWorld.SnakesList[snakeID];
                    if (snake.frameCounter == null) { snake.frameCounter = 0; }

                    // Move the snake forward
                    theWorld.getCurrentSnakeHitboxes();
                    if (!theWorld.checkIfEatingFood(snake) && snake.frameCounter == 0)
                    {

                        snake.frameCounter = 0;
                        theWorld.wraparound(snake);
                        theWorld.moveHeadForward(snake, true);


                    }
                    if (theWorld.checkIfEatingFood(snake) || snake.frameCounter != 0)
                    {
                        if (snake.frameCounter == 0) { snake.score++; }
                        theWorld.moveHeadForward(snake, false);
                        snake.frameCounter++;
                        if (snake.frameCounter == setting.snakeGrowth)
                        {
                            snake.frameCounter = 0;
                        }
                    }
                    theWorld.checkIfTailCaughtUp(snake);

                    // Check if the snake is in collision with any obstacles or boundaries
                    if (theWorld.checkIfInHitbox(snake, theWorld.WallHitboxes) || theWorld.checkIfInHitbox(snake, theWorld.SnakeHitboxes))
                    {
                        // Mark the snake as died and no longer alive
                        snake.died = true;
                        snake.alive = false;

                        Respawn(snake);
                        break;
                    }

                }


            }
        }
        /// <summary>
        /// Respawns a food item in the game world after a delay
        /// </summary>
        /// <param name="food">The food object to be respawned</param>
        private async void RespawnFood(Powerup food)
        {
            // Run the respawn logic in a separate task to avoid blocking the UI thread
            await Task.Run(async () =>
            {
                // Wait for the specified delay before respawning the food
                await Task.Delay(setting!.powerupsDelay);

                // Locking 'theWorld' to ensure thread safety while modifying shared resources
                lock (theWorld)
                {
                    // Call the method to respawn the food in the game world
                    theWorld.respawnFood(food);
                }
            });
        }

        /// <summary>
        /// Respawns a snake in the game world after it has died, following a specified delay
        /// </summary>
        /// <param name="snake">The snake object to be respawned</param>
        private async void Respawn(Snake snake)
        {
            // Run the respawn logic in a separate task to avoid blocking the UI thread
            await Task.Run(async () =>
            {
                // Locking 'theWorld' to ensure thread safety while modifying shared resources
                lock (theWorld)
                {
                    // Validate and possibly update the snake before marking it as dead
                    snake = validSnake(snake.snake, snake.name);
                    snake.alive = false;
                    snake.died = true;
                }

                // Wait for the specified delay before respawning the snake
                await Task.Delay(setting!.respawnRate);

                // Locking 'theWorld' again to safely update the snake's state
                lock (theWorld)
                {
                    // Mark the snake as alive again and reset its 'died' state
                    snake.alive = true;
                    snake.died = false;
                }
            });
        }


        /// <summary>
        /// Retrieves the World object associated with this instanc
        /// </summary>
        /// <returns>The World object associated with this instance</returns>
        public World getWorld()
        {
            return theWorld;
        }


        /// <summary>
        /// Creates a valid Snake object with a random position and direction
        /// </summary>
        /// <param name="ID">The ID of the Snake</param>
        /// <param name="Name">The name of the Snake</param>
        /// <returns>A valid Snake object</returns>
        internal Snake validSnake(int ID, string Name)
        {
            Random randomPos = new Random();
            int snakeLength = setting.startingLength;
            // Keep generating a Snake until a valid one is found
            while (true)
            {
                float randomX = randomPos.Next(-theWorld.WorldSize / 2 + 25, theWorld.WorldSize / 2 - 25);
                float randomY = randomPos.Next(-theWorld.WorldSize / 2 + 25, theWorld.WorldSize / 2 - 25);
                List<Vector2D> body = new List<Vector2D>() { new Vector2D(randomX, randomY),
            new Vector2D(randomX + snakeLength, randomY) };
                int ranDir = randomPos.Next(0, 4); // Generate a random direction (0-3)

                Snake snake = new Snake(ID, Name, body, new Vector2D(1, 0), 0, false, true, false, true); // Create a fake snake

                // Override the fake snake with a valid direction based on ranDir
                if (ranDir == 0)
                { // Spawn facing right
                    body = new List<Vector2D>() { new Vector2D(randomX + snakeLength, randomY),
                new Vector2D(randomX, randomY) };
                    snake = new Snake(ID, Name, body, new Vector2D(1, 0), 0, false, true, false, true);
                }
                else if (ranDir == 1)
                {// Spawn facing down
                    body = new List<Vector2D>() { new Vector2D(randomX, randomY),
                new Vector2D(randomX, randomY - snakeLength) };
                    snake = new Snake(ID, Name, body, new Vector2D(0, 1), 0, false, true, false, true);
                }
                else if (ranDir == 2)
                { // Spawn facing left
                    body = new List<Vector2D>() { new Vector2D(randomX - snakeLength, randomY),
                new Vector2D(randomX, randomY) };
                    snake = new Snake(ID, Name, body, new Vector2D(-1, 0), 0, false, true, false, true);
                }
                else if (ranDir == 3)
                { // Spawn facing up
                    body = new List<Vector2D>() { new Vector2D(randomX, randomY - snakeLength),
                new Vector2D(randomX, randomY) };
                    snake = new Snake(ID, Name, body, new Vector2D(0, -1), 0, false, true, false, true);
                }

                // Create an inverted snake to check for collisions
                body.Reverse();
                Snake invertedSnake = new Snake(ID, Name, body, new Vector2D(1, 0), 0, false, true, false, true);

                // Check if both the snake and inverted snake are not in collision with any existing objects
                if (!theWorld.checkIfInHitbox(snake, theWorld.WallHitboxes) && !theWorld.checkIfInHitbox(invertedSnake, theWorld.WallHitboxes))
                {
                    theWorld.SnakesList[snake.snake] = snake;
                    return snake; // Return the valid snake
                }
            }
        }

    }
}
