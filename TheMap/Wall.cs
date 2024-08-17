using SnakeGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace SnakeGame.TheMap
{
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
* Description: Represents the game Walls, containing snakes, powerups, walls, and a leaderboard
 */

    public class Wall
    {
        public int wall { get; set; }

        public Vector2D p1 { get; set; }
        public Vector2D p2 { get; set; }

        [JsonConstructor]
        public Wall(int wall, Vector2D p1, Vector2D p2)
        {
            this.wall = wall;
            this.p1 = p1;
            this.p2 = p2;
        }


        public double[] getHitbox()
        {
            double hitboxTopY = 0;
            double hitboxBottomY = 0;
            double hitboxRightX = 0;
            double hitboxLeftX = 0;
            if (this.p1.Y == this.p2.Y)//the wall is horizontal
            {

                hitboxTopY = this.p1.Y - 25; 
                hitboxBottomY=this.p1.Y + 25;
                if (this.p1.X < this.p2.X) //this means the P1 is on the left
                {
                    hitboxLeftX = this.p1.X - 25;
                    hitboxRightX = this.p2.X + 25;
                }
                else if(this.p1.X > this.p2.X) //this means the P1 is on the right.
                {
                    hitboxLeftX = this.p2.X - 25;
                    hitboxRightX = this.p1.X + 25;
                }

            }
            else if (this.p1.X == this.p2.X)//the wall is vertical
            {
                if (this.p1.Y < this.p2.Y)//this means P1 is higher up than P2.
                {
                    hitboxTopY = this.p1.Y - 25;
                    hitboxBottomY = this.p2.Y + 25;
                }
                else if (this.p1.Y > this.p2.Y) //this means P2 is on the top.
                {
                    hitboxTopY = this.p2.Y - 25;
                    hitboxBottomY = this.p1.Y + 25;
                }

                hitboxLeftX = this.p1.X - 25;
                hitboxRightX = this.p2.X + 25;
            }

            double[] wallBorders = new double[4];
            wallBorders[0] = hitboxTopY;
            wallBorders[1] = hitboxBottomY;
            wallBorders[2] = hitboxLeftX;
            wallBorders[3] = hitboxRightX;
            return wallBorders;
        }
    }
}
