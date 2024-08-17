using SnakeGame.TheMap;
using System.Collections;
using System.Numerics;
using System.Collections.Generic;

namespace SnakeGame;
/**
* University of Utah Course: CS 3500-001 Fall 2023 Software Practice
*
* Semester: Fall 2023
*
* Assignment: 8
*
* @Author: Duy Khanh Tran & Orion Harmon
*
* Created date: 11/27/2023
*
* Description: Represents the game world, containing snakes, powerups, walls, and a leaderboard
 */
public class World
{
    public bool shrinkSnake { get; set; }
    public int speed { get; set; }
    public int foodCount { get; set; }
    /// <summary>
    /// Stores a comprehensive list of all the Wall hitboxes. 
    /// </summary>
    public List<double[]> WallHitboxes;

    /// <summary>
    /// Stores a comprehensive list of all the Snake hitboxes. 
    /// </summary>
    public List<double[]> SnakeHitboxes;


    /// <summary>
    /// Stores all snakes in the game
    /// </summary>
    public Dictionary<int, Snake> SnakesList;

    /// <summary>
    /// Stores all powerups in the game
    /// </summary>
    public Dictionary<int, Powerup> PowerupsList;

    /// <summary>
    /// Stores all walls in the game
    /// </summary>
    public Dictionary<int, Wall> WallsList;



    /// <summary>
    /// An array representing the top snakes in the leaderboard
    /// </summary>
    public Snake[] LeaderBoard;


    /// <summary>
    /// A list of scores in the game
    /// </summary>
    public List<int> scores;
    private int? universeSize;

    /// <summary>
    /// Gets or sets the size of the world
    /// </summary>
    public int WorldSize { get; set; }

    /// <summary>
    /// Gets or sets the ID of the current snake
    /// </summary>
    public int SnakeID { get; set; }

    /// <summary>
    /// Gets the size of the world, The setter is private to prevent external modification
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Initializes a new instance of the World class with a specified size
    /// </summary>
    /// <param name="_size">The size of the world</param>
    public World(int _size)
    {
        SnakeHitboxes = new List<double[]>();
        // Initialize dictionaries for snakes, powerups, and walls
        SnakesList = new Dictionary<int, Snake>();
        PowerupsList = new Dictionary<int, Powerup>();
        WallsList = new Dictionary<int, Wall>();
        WallHitboxes = new List<double[]>();

        // Set the size of the world
        Size = _size;
        WorldSize = 2000;
        // Initialize the list of scores and the leaderboard
        scores = new List<int>();
        LeaderBoard = new Snake[5];

    }

    public World(int? universeSize)
    {
        this.universeSize = universeSize;
    }

    public void getCurrentSnakeHitboxes()
    {
        List<double[]> allSnakeHitboxes = new List<double[]>();
        lock (SnakesList)
        {
            foreach (int snakeID in SnakesList.Keys)
            {
                SnakesList.TryGetValue(snakeID, out Snake snake);
                List<double[]> hitboxesToAdd = snake.getHitboxes();
                int counter = 0;
                while (counter < hitboxesToAdd.Count())
                {
                    double[] hitbox = hitboxesToAdd[counter];
                    if (Math.Abs(hitbox[0] - hitbox[1]) > WorldSize || Math.Abs(hitbox[2] - hitbox[3]) > WorldSize)
                    {
                        hitboxesToAdd.Remove(hitbox);
                        break;
                    }
                    counter++;
                }

                allSnakeHitboxes = allSnakeHitboxes.Concat(hitboxesToAdd).ToList();
            }
        }
        this.SnakeHitboxes = allSnakeHitboxes;
    }

    /// <summary>
    /// This method builds the hitboxes from all the walls and stores them in the list of hitboxes to iterate through when checking snake's position. 
    /// </summary>
    public void buildHitboxesWalls()
    {
        foreach (int wall in WallsList.Keys)
        {
            WallsList.TryGetValue(wall, out Wall? value);
            double[] hitbox = value!.getHitbox();

            //The below is optimization code not "essential". im splitting the world into 50X wide segments to narrow the scope of collision detection on at least
            //one axis. should dramatically speed up runtime. 
            if (Math.Abs(hitbox[2] - hitbox[3]) > 50) //if this is greater than 50, then the wall is horizontal. splice it into X segment chunks. 
            {
                int counterX = 0;
                while (counterX < Math.Abs(hitbox[2] - hitbox[3]))
                {
                    double[] hitboxChunk = new double[4];
                    hitboxChunk[0] = hitbox[0];
                    hitboxChunk[1] = hitbox[1];
                    hitboxChunk[2] = hitbox[2] + counterX;
                    hitboxChunk[3] = hitbox[2] + (counterX + 50);
                    counterX = counterX + 50;
                    WallHitboxes.Add(hitboxChunk);

                }
            }
            else
            {
                WallHitboxes.Add(hitbox);
            }
        }

        //this is some epic level optimization. Im sorting the wall hitbox's by their X components, so that when im scanning for collisions I can check only the 
        //hitboxes that are close (by X slice) to the object im checking for. 
        WallHitboxes.Sort((hitbox1, hitbox2) => hitbox1[3].CompareTo(hitbox2[3]));

    }

    /// <summary>
    /// Checks if the snake is eating any food
    /// </summary>
    /// <param name="snake">The snake object to check for eating food</param>
    /// <returns>True if the snake is eating food; otherwise, false</returns>
    public bool checkIfEatingFood(Snake snake)
    {
        if (snake.body.Count() > 1)
        {


            float snakeHeadX = (float)snake.body.Last<Vector2D>().GetX();
            float snakeHeadY = (float)snake.body.Last<Vector2D>().GetY();

            // Lock to ensure thread safety while accessing shared resources
            lock (this)
            {
                // Iterate through all food items
                foreach (int foodID in PowerupsList.Keys)
                {
                    PowerupsList.TryGetValue(foodID, out Powerup? food);
                    // Calculate the distance between the snake head and the food
                    float distanceXToFood = Math.Abs((float)food!.loc.X - snakeHeadX);
                    float distanceYToFood = Math.Abs((float)food.loc.Y - snakeHeadY);

                    // Check if the snake head is close enough to the food
                    if (distanceXToFood < 10 && distanceYToFood < 10)
                    {
                        food.died = true;
                        // Mark the food as eaten
                        // PowerupsList.Remove(foodID); // Uncomment if you want to remove the food from the list
                        return true; // The snake is eating the food
                    }
                }
            }
        }
        return false; // The snake is not eating any food
    }

    /// <summary>
    /// Handles wraparound logic for a snake at the boundaries of the world
    /// </summary>
    /// <param name="snake">The snake to apply wraparound logic to</param>
    public void wraparound(Snake snake)
    {
        if (snake.body.Count > 1)
        {


            // Check for horizontal boundary crossing
            if (Math.Abs(snake.body.Last<Vector2D>().X) >= WorldSize / 2)
            {
                // Create a new vector for the wraparound effect
                Vector2D wraparound = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                double amountOutsideWorld = Math.Abs((WorldSize / 2) - Math.Abs(snake.body.Last<Vector2D>().X));
                if (snake.body.Last<Vector2D>().X < 0)
                {
                    //above the right boundary of world.
                    amountOutsideWorld = -amountOutsideWorld;
                }
                snake.body.Add(wraparound);

                // Invert the X-coordinate for wraparound


                snake.body.Last<Vector2D>().X = -snake.body.Last<Vector2D>().X + amountOutsideWorld;
                wraparound = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                snake.body.Add(wraparound);
            }

            // Check for vertical boundary crossing

            if (Math.Abs(snake.body.Last<Vector2D>().Y) >= WorldSize / 2)
            {
                // Create a new vector for the wraparound effect
                Vector2D wraparound = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                double amountOutsideWorld = Math.Abs((WorldSize / 2) - Math.Abs(snake.body.Last<Vector2D>().Y));
                if (snake.body.Last<Vector2D>().Y < 0)
                {
                    //above the top boundary of world.
                    amountOutsideWorld = -amountOutsideWorld;
                }

                snake.body.Add(wraparound);

                // Invert the Y-coordinate for wraparound


                snake.body.Last<Vector2D>().Y = -snake.body.Last<Vector2D>().Y + amountOutsideWorld;
                wraparound = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                snake.body.Add(wraparound);
            }
        }
    }

    /// <summary>
    /// Checks if the tail of the snake has caught up to its head
    /// </summary>
    /// <param name="snake">The snake to check for tail catch-up</param>
    public void checkIfTailCaughtUp(Snake snake)
    {
        // Ensure the snake has more than one segment
        if (snake.body.Count > 1)
        {
            // Check for close proximity in the X direction
            if (snake.body[0].X != snake.body[1].X)
                if (Math.Abs(snake.body.First<Vector2D>().X - snake.body[1].X) < 5)
                    snake.body.RemoveAt(0); // Remove the head segment if too close to the next segment

            // Check for close proximity in the Y direction
            if (snake.body[0].Y != snake.body[1].Y)
                if (Math.Abs(snake.body.First<Vector2D>().Y - snake.body[1].Y) < 5)
                    snake.body.RemoveAt(0); // Remove the head segment if too close to the next segment
        }
    }


    /// <summary>
    /// 
    /// this is the main logic to determine if something is within a hitbox. used for spawning food (might be removed for optimization later), and snake collisions. 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool checkIfInHitbox(Object o, List<double[]> hitboxList)
    {

        if (o.GetType() == typeof(Powerup))
        {
            Powerup food = (Powerup)o;
            lock (this)
                foreach (double[] hitbox in hitboxList)
                {
                    double topBoundary = hitbox[0];
                    double bottomBoundary = hitbox[1];
                    double leftBoundary = hitbox[2];
                    double rightBoundary = hitbox[3];

                    if (food.loc.Y > topBoundary - 15 && food.loc.Y < bottomBoundary + 15 && food.loc.X > leftBoundary - 15 && food.loc.X < rightBoundary + 15)
                    {
                        //then it is within the boundaries of the hitbox!! killll it!!!!
                        return true;
                    }

                }
        }

        if (o.GetType() == typeof(Snake))
        {
            Snake snake = (Snake)o;
            if (snake.body.Count== 0) { return true; }
            float snakeHeadX = (float)snake.body.Last<Vector2D>().GetX();
            if (snake.dir.X == 1)
            {
                //snake is moving to right. add 4 so its outside its own hitbox.
                snakeHeadX = snakeHeadX + 4;
            }
            else
            {
                //snake is moving left. subtract 4 so its outside its own hitbox.
                snakeHeadX = snakeHeadX - 4;
            }
            float snakeHeadY = (float)snake.body.Last<Vector2D>().GetY();
            if (snake.dir.Y == 1)
            {
                //snake is moving down add 4 so its outside its own hitbox. 
                snakeHeadY = snakeHeadY + 4;
            }
            else
            {
                //snake is moving up. subtract 4 so its outside its own hitbox. 
                snakeHeadY = snakeHeadY - 4;
            }
            if (Math.Abs(snakeHeadX) > (WorldSize + 50 / 2) || Math.Abs(snakeHeadY) > (WorldSize + 50 / 2)) { return true; }

            foreach (double[] hitbox in WallHitboxes)
            {
                double distanceToHitbox = Math.Abs(hitbox[2] - snakeHeadX);
                if (Math.Abs(distanceToHitbox) > 75)
                {
                    //the wall we're about to check is too far away to matter. skip it.
                    continue;
                }

                double topBoundary = hitbox[0];
                double bottomBoundary = hitbox[1];
                double leftBoundary = hitbox[2];
                double rightBoundary = hitbox[3];


                if (snakeHeadY >= topBoundary && snakeHeadY <= bottomBoundary && snakeHeadX >= leftBoundary && snakeHeadX <= rightBoundary)
                {
                    //then it is within the boundaries of the hitbox!! killll it!!!!
                    return true;
                }

            }
            if (SnakeHitboxes != null)
                foreach (double[] hitbox in SnakeHitboxes)
                {
                    double topBoundary = hitbox[0];
                    double bottomBoundary = hitbox[1];
                    double leftBoundary = hitbox[2];
                    double rightBoundary = hitbox[3];


                    if (snakeHeadY > topBoundary && snakeHeadY < bottomBoundary && snakeHeadX > leftBoundary && snakeHeadX < rightBoundary)
                    {
                        //then it is within the boundaries of the hitbox!! killll it!!!!
                        return true;
                    }

                }

        }




        return false;
    }
    public void respawnFood(Powerup power)
    {
        lock (PowerupsList)
        {

            //make location of new powerup
            Random rand = new Random();
            int randomX = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24); //random coords inside of world boundaries (factoring in wall width)
            int randomY = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24);
            Vector2D emptyLocation = new Vector2D(randomX, randomY);
            Powerup newFood = new Powerup(69, emptyLocation, false); //69 doesnt matter. overwritten anyways.
            int counter = 1;
            while (counter < PowerupsList.Keys.Count()) //this loops checks to make sure the new food doesnt overlap existing food. 
            {
                randomX = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24); //random coords inside of world boundaries (factoring in wall width)
                randomY = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24);

                emptyLocation = new Vector2D(randomX, randomY); //overwrite with random
                                                                //double foodToCompareX=PowerupsList.TryGetValue
                newFood = new Powerup(PowerupsList.Count() + 1, emptyLocation, false);

                double distanceToExistingFoodX = Math.Abs(Math.Abs(PowerupsList[counter].loc.X) - Math.Abs(emptyLocation.X));
                double distanceToExistingFoodY = Math.Abs(Math.Abs(PowerupsList[counter].loc.Y) - Math.Abs(emptyLocation.Y));
                //note, this will make a small bug where food will not spawn on the inverse of the map relative to the existing food. 

                if (distanceToExistingFoodX < 10 || distanceToExistingFoodY < 10 || checkIfInHitbox(newFood, WallHitboxes))
                {
                    counter = 0;
                    //restart/try the loop again. its too close to existing food. 
                }

                counter++;
            }
            //if made it past that loop, the food is not near any other existing food, and can be added to the world. 
            power.died = false;
            power.loc = emptyLocation;
            //need to also search through all the walls to make sure doesnt spawn under a wall.


        }
    }


    /// <summary>
    /// This method respawns Food on tha map as its eaten. 
    /// </summary>
    public void spawnFood()
    {

        lock (PowerupsList)
        {
            while (PowerupsList.Keys.Count() < foodCount) //if there are fewer than "foodcount" food(s) in the world. 
            {
                //make location of new powerup
                Random rand = new Random();
                int randomX = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24); //random coords inside of world boundaries (factoring in wall width)
                int randomY = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24);
                Vector2D emptyLocation = new Vector2D(randomX, randomY);
                Powerup newFood = new Powerup(69, emptyLocation, false); //69 doesnt matter. overwritten anyways.
                int counter = 1;
                while (counter < PowerupsList.Keys.Count()) //this loops checks to make sure the new food doesnt overlap existing food. 
                {
                    randomX = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24); //random coords inside of world boundaries (factoring in wall width)
                    randomY = rand.Next(-WorldSize / 2 + 25, WorldSize / 2 - 24);

                    emptyLocation = new Vector2D(randomX, randomY); //overwrite with random
                                                                    //double foodToCompareX=PowerupsList.TryGetValue
                    newFood = new Powerup(PowerupsList.Count() + 1, emptyLocation, false);

                    double distanceToExistingFoodX = Math.Abs(Math.Abs(PowerupsList[counter].loc.X) - Math.Abs(emptyLocation.X));
                    double distanceToExistingFoodY = Math.Abs(Math.Abs(PowerupsList[counter].loc.Y) - Math.Abs(emptyLocation.Y));
                    //note, this will make a small bug where food will not spawn on the inverse of the map relative to the existing food. 

                    if (distanceToExistingFoodX < 10 || distanceToExistingFoodY < 10 || checkIfInHitbox(newFood, WallHitboxes))
                    {
                        counter = 0;
                        //restart/try the loop again. its too close to existing food. 
                    }

                    counter++;
                }
                //if made it past that loop, the food is not near any other existing food, and can be added to the world. 

                //need to also search through all the walls to make sure doesnt spawn under a wall.

                newFood = new Powerup(PowerupsList.Count() + 1, emptyLocation, false);
                PowerupsList[newFood.power] = newFood;
            }
        }
    }

    public List<Vector2D> moveHeadForward(Snake snake, bool moveTailAlso)
    {
        if (snake.body.Count == 0)
        {
            snake.alive = false;
            snake.died = true;
            return new List<Vector2D>() ;
        }
        //if (snake.body.Count > 1)
        {


            List<Vector2D> updatedBody = new List<Vector2D>();
            updatedBody = snake.body;



            //move the head (speed) units wherever it is pointing.
            Vector2D head = updatedBody[updatedBody.Count - 1];
            if (snake.dir.X < 0)
            {
                head.X = head.X - speed;
                updatedBody[updatedBody.Count - 1] = head;
            } //snake is moving left

            else if (snake.dir.X > 0)
            {
                head.X = head.X + speed;
                updatedBody[updatedBody.Count - 1] = head;
            } //snake is moving right
            else if (snake.dir.Y < 0)
            {
                head.Y = head.Y - speed;
                updatedBody[updatedBody.Count - 1] = head;
            } //snake is moving up
            else if (snake.dir.Y > 0)
            {
                head.Y = head.Y + speed;
                updatedBody[updatedBody.Count - 1] = head;
            } //snake is moving down
            if (moveTailAlso)
            {
                updatedBody = moveTail(snake, updatedBody, speed);

            }
            return updatedBody;
        }
        //else { return new List<Vector2D>(); }
    }
    private List<Vector2D> moveTail(Snake snake, List<Vector2D> updatedBody, double tailSpeed)
    {

        Vector2D tail = updatedBody[0]; //grabs the end tail
        if (updatedBody.Count == 1)
        {
            snake.died = true;
            snake.alive = false;
            return updatedBody;
        }
        Vector2D beforeTail = updatedBody[1]; //grabs the node just before the last (tail). 
        //move the tail (speed) units in whatever direction shortens it. 
        //find the direction last segment is going. \
        double segmentLengthX = Math.Abs(beforeTail.X - tail.X);
        double segmentLengthY = Math.Abs(beforeTail.Y - tail.Y);

        Boolean wraparound = false;
        if (segmentLengthX > WorldSize - 5 || segmentLengthY > WorldSize - 5)
        {
            //a wraparound on X axis has occured. 
            //make it so the below logic is inverted somehow. 
            wraparound = true;
        }

        if (tail.X == beforeTail.X) //segment is vertical
        {
            if (!wraparound)
            {
                if (tail.Y < beforeTail.Y) //tail is "pointing" downward
                {
                    //shrink it from top to bottom. 
                    if (Math.Abs(tail.Y - beforeTail.Y) < 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X, updatedBody[0].Y + tailSpeed); }
                }
                else if (tail.Y >= beforeTail.Y) //tail is "pointing" upward
                {
                    //shrink it from bottom to top
                    if (Math.Abs(tail.Y - beforeTail.Y) < 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X, updatedBody[0].Y - tailSpeed); }


                }
            }
            if (wraparound)
            {
                if (tail.Y > beforeTail.Y) //tail is "pointing" downward
                {
                    //shrink it from top to bottom. 
                    if (Math.Abs(tail.Y - beforeTail.Y) < 10 || Math.Abs(tail.Y - beforeTail.Y) > WorldSize - 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X, updatedBody[0].Y + tailSpeed); }
                }
                else if (tail.Y <= beforeTail.Y) //tail is "pointing" upward
                {
                    //shrink it from bottom to top
                    if (Math.Abs(tail.Y - beforeTail.Y) < 10 || Math.Abs(tail.Y - beforeTail.Y) > WorldSize - 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X, updatedBody[0].Y - tailSpeed); }


                }
            }


        }
        if (!wraparound)
        {
            if (tail.Y == beforeTail.Y) //segment is horizontal
            {
                if (tail.X < beforeTail.X) //tail is "pointing" right (end of tail is left)
                {
                    //shrink it from left to right.
                    if (Math.Abs(tail.X - beforeTail.X) < 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X + tailSpeed, updatedBody[0].Y); }

                }
                else if (tail.X >= beforeTail.X) //tail is "pointing" left (end of tail is right)
                {
                    //shrink it from right to left. 
                    if (Math.Abs(tail.X - beforeTail.X) < 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X - tailSpeed, updatedBody[0].Y); }

                }
            }
        }
        if (wraparound)
        {
            if (tail.Y == beforeTail.Y) //segment is horizontal
            {
                if (tail.X > beforeTail.X) //tail is "pointing" right (end of tail is left)
                {
                    //shrink it from left to right.
                    if (Math.Abs(tail.X - beforeTail.X) < 10 || Math.Abs(tail.X - beforeTail.X) > WorldSize - 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X + tailSpeed, updatedBody[0].Y); }

                }
                else if (tail.X <= beforeTail.X) //tail is "pointing" left (end of tail is right)
                {
                    //shrink it from right to left. 
                    if (Math.Abs(tail.X - beforeTail.X) < 10 || Math.Abs(tail.X - beforeTail.X) > WorldSize - 10) //this means shrunken segment has reached next node. 
                    {
                        updatedBody.Remove(updatedBody[0]);
                    }
                    else { updatedBody[0] = new Vector2D(updatedBody[0].X - tailSpeed, updatedBody[0].Y); }

                }
            }
        }

        return updatedBody;
    }


    /// <summary>
    /// This method changes the direction of the input snake to the new direction given. valid inputs are "up, down, right, left".
    /// 
    /// </summary>
    /// <param name="snake"></param>
    /// <param name="direction"></param>
    public void changeSnakeDirection(long snakeID, String direction)
    {
        Snake snake = SnakesList[(int)snakeID];

        double tailSpeed = -3.1;
        if (shrinkSnake)
        {
            tailSpeed = 5;
        }
        if (direction.Equals("down") && (snake.dir.Y != 1 && snake.dir.Y != -1))
        {
            // lock (SnakesList[(int)snakeID])
            {
                Vector2D newBaseNode = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                Vector2D newHead = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);

                snake.body[snake.body.Count - 1] = newBaseNode; //sets the last node to the previous head.
                snake.body.Add(newHead);
                snake.dir = new Vector2D(0, 1); //change direction.
                snake.body = moveTail(snake, snake.body, tailSpeed);
            }


        }
        else if (direction.Equals("up") && (snake.dir.Y != -1 && snake.dir.Y != 1))
        {
            // lock (SnakesList[(int)snakeID])
            {
                Vector2D newBaseNode = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                Vector2D newHead = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);


                snake.body[snake.body.Count - 1] = newBaseNode; //sets the last node to the previous head.
                snake.body.Add(newHead);
                snake.dir = new Vector2D(0, -1); //change direction.
                snake.body = moveTail(snake, snake.body, tailSpeed);
            }

        }
        else if (direction.Equals("left") && (snake.dir.X != -1 && snake.dir.X != 1))
        {
            //lock (SnakesList[(int)snakeID])
            {
                Vector2D newBaseNode = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                Vector2D newHead = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);

                snake.body[snake.body.Count - 1] = newBaseNode; //sets the last node to the previous head.
                snake.body.Add(newHead);
                snake.dir = new Vector2D(-1, 0); //change direction.
                snake.body = moveTail(snake, snake.body, tailSpeed);
            }


        }
        else if (direction.Equals("right") && (snake.dir.X != 1 && snake.dir.X != -1))
        {
            //lock (SnakesList[(int)snakeID])
            {
                Vector2D newBaseNode = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);
                Vector2D newHead = new Vector2D(snake.body.Last<Vector2D>().X, snake.body.Last<Vector2D>().Y);

                snake.body[snake.body.Count - 1] = newBaseNode; //sets the last node to the previous head.
                snake.body.Add(newHead);
                snake.dir = new Vector2D(1, 0); //change direction.
                snake.body = moveTail(snake, snake.body, tailSpeed);
            }


        }

    }
}


