using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
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
* Description: Represents the game Powerups, containing snakes, powerups, walls, and a leaderboard
 */
namespace SnakeGame.TheMap
{
    public class Powerup
    {
        public Vector2D loc { get; set; }
        public int power { get; set; }
        public bool died { get; set; }

        [JsonConstructor]
        public Powerup(int power, Vector2D loc, bool died)
        {
            this.power = power;
            this.loc = loc;
            this.died = died;
        }
       


    }
}
