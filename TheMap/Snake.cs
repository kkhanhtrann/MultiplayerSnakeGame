using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
* Description: Represents the game Snakes, containing snakes, powerups, walls, and a leaderboard
 */

namespace SnakeGame.TheMap
{
    /// <summary>
    /// Represents a snake in the game
    /// </summary>
    public class Snake
    {
        [JsonIgnore]
        public Tuple<int, int, int> ColorCode { get; set; }
        /// <summary>
        /// Unique identifier for the snake
        /// </summary>
        public int snake { get; set; }
        /// <summary>
        /// a int to keep track of frames for growing the snake.
        /// </summary>
        public int frameCounter { get; set; }

        /// <summary>
        /// Name of the player controlling the snake
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// List of Vector2D points representing the snake's body
        /// The first point is the tail, and the last point is the head
        /// </summary>
        public List<Vector2D> body { get; set; }

        /// <summary>
        /// Current orientation of the snake as a Vector2D
        /// It is an axis-aligned vector (purely horizontal or vertical)
        /// </summary>
        public Vector2D dir { get; set; }

        /// <summary>
        /// Player's score, representing the number of powerups eaten by the snake
        /// </summary>
        public int score { get; set; }

        /// <summary>
        /// Indicates if the snake died in the current frame
        /// True only on the frame in which the snake dies
        /// </summary>
        public bool died { get; set; }

        /// <summary>
        /// Indicates whether the snake is alive or dead
        /// Helpful for determining whether to draw the snake
        /// </summary>
        public bool alive { get; set; }

        /// <summary>
        /// Indicates if the player controlling the snake disconnected in the current frame
        /// This is true only once, when the disconnection occurs
        /// </summary>
        public bool dc { get; set; }

        /// <summary>
        /// Indicates if the player joined the game in the current frame
        /// True only for the frame in which the player joins
        /// </summary>
        public bool join { get; set; }


        [JsonConstructor]
        public Snake(int snake, string name, List<Vector2D> body, Vector2D dir, int score, bool died, bool alive, bool dc, bool join)
        {

            Random random = new Random();
            ColorCode = new Tuple<int, int, int>(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256));
            this.snake = snake;
            this.name = name;
            this.body = body;
            this.dir = dir;
            this.score = score;
            this.died = died;
            this.alive = alive;
            this.dc = dc;
            this.join = join;
        }

        public List<double[]> getHitboxes()
        {
            //step 1. make a list of segments that make up the snake from the body. 
            //number of segments = snake.body.Count-1.
            //so to get the first segment, (tail), do snake.body[snake.body.count] to snake.body[snake.body.count-1)
            List<double[]> hitboxes = new List<double[]>();
            int counter = 0;
            while (counter < body.Count - 1)
            {
                Vector2D startpoint = this.body[counter];
                Vector2D endpoint = this.body[counter + 1];
                hitboxes.Add(getSegmentHitbox(startpoint, endpoint));
                counter++;
            }



            return hitboxes;

        }

        private double[] getSegmentHitbox(Vector2D p1, Vector2D p2)
        {

            double hitboxTopY = 0;
            double hitboxBottomY = 0;
            double hitboxRightX = 0;
            double hitboxLeftX = 0;
            if (p1.Y == p2.Y)//the segment is horizontal
            {

                hitboxTopY = p1.Y - 4;
                hitboxBottomY = p1.Y + 4;
                if (p1.X < p2.X) //this means the P1 is on the left
                {
                    hitboxLeftX = p1.X - 4;
                    hitboxRightX = p2.X + 4;
                }
                else if (p1.X > p2.X) //this means the P1 is on the right.
                {
                    hitboxLeftX = p2.X - 4;
                    hitboxRightX = p1.X + 4;
                }

            }
            else if (p1.X == p2.X)//the segment is vertical
            {
                if (p1.Y < p2.Y)//this means P1 is higher up than P2.
                {
                    hitboxTopY = p1.Y - 4;
                    hitboxBottomY = p2.Y + 4;
                }
                else if (p1.Y > p2.Y) //this means P2 is on the top.
                {
                    hitboxTopY = p2.Y - 4;
                    hitboxBottomY = p1.Y + 4;
                }

                hitboxLeftX = p1.X - 4;
                hitboxRightX = p2.X + 4;
            }

            double[] segmentBorders = new double[4];
            segmentBorders[0] = hitboxTopY;
            segmentBorders[1] = hitboxBottomY;
            segmentBorders[2] = hitboxLeftX;
            segmentBorders[3] = hitboxRightX;
            return segmentBorders;
        }



    }
}
