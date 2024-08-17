using System;
using System.Linq;
using System.Xml.Linq;
using SnakeGame.TheMap;
using SnakeGame;
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
* Description: Represents the game ServerSetting
 */
namespace Server
{
    public class GameSettings
    {
        // Properties for various game settings
        public int msPerFrame { get; private set; }
        public int respawnRate { get; private set; }
        public int universeSize { get; private set; }
        public int snakeSpeed { get; private set; }
        public int startingLength { get; private set; }
        public int snakeGrowth { get; private set; }
        public int maxPowerups { get; private set; }
        public int powerupsDelay { get; private set; }
        public bool shrinkSnake { get; private set; }
        public bool wrapAround { get; private set; }

        // List to store the walls in the game
        public List<Wall> wallsList { get; private set; }

        // Constructor to initialize the game settings from an XML file
        public GameSettings(string FileName)
        {
            wallsList = new List<Wall>();
            try
            {
                // Read the XML content from the file
                string xmlContent = File.ReadAllText(FileName);
                XDocument doc = XDocument.Parse(xmlContent);

                // Extract the 'GameSettings' element
                XElement? gameSettingsElement = doc.Element("GameSettings");
                if (gameSettingsElement != null)
                {
                    // Parse individual settings, using default values if not found
                    msPerFrame = int.Parse(gameSettingsElement.Element("MSPerFrame")?.Value ?? "34");
                    respawnRate = int.Parse(gameSettingsElement.Element("RespawnRate")?.Value ?? "100");
                    universeSize = int.Parse(gameSettingsElement.Element("UniverseSize")?.Value ?? "2000");
                    snakeSpeed = int.Parse(gameSettingsElement.Element("SnakeSpeed")?.Value ?? "6");
                    startingLength = int.Parse(gameSettingsElement.Element("StartingLength")?.Value ?? "120");
                    snakeGrowth = int.Parse(gameSettingsElement.Element("SnakeGrowth")?.Value ?? "24");
                    maxPowerups = int.Parse(gameSettingsElement.Element("MaxPowerups")?.Value ?? "20");
                    powerupsDelay = int.Parse(gameSettingsElement.Element("PowerupsDelay")?.Value ?? "75");
                    shrinkSnake = bool.Parse(gameSettingsElement.Element("ShrinkSnake")?.Value ?? "true");
                    wrapAround = bool.Parse(gameSettingsElement.Element("WrapAround")?.Value ?? "true");

                    int mainWall = 0;
                    // Extract wall settings
                    XElement? wallsElement = gameSettingsElement.Element("Walls");
                    if (wallsElement != null)
                    {
                        foreach (XElement wallElement in wallsElement.Elements("Wall"))
                        {
                            if (mainWall < 4 && wrapAround)
                            {
                                mainWall++;
                                continue;
                            }
                            // Parse wall information and add to the walls list
                            int wallId = int.Parse(wallElement.Element("ID")?.Value ?? "0");
                            int p1X = int.Parse(wallElement.Element("p1")?.Element("x")?.Value ?? "0");
                            int p1Y = int.Parse(wallElement.Element("p1")?.Element("y")?.Value ?? "0");
                            Vector2D p1 = new Vector2D(p1X, p1Y);
                            int p2X = int.Parse(wallElement.Element("p2")?.Element("x")?.Value ?? "0");
                            int p2Y = int.Parse(wallElement.Element("p2")?.Element("y")?.Value ?? "0");
                            Vector2D p2 = new Vector2D(p2X, p2Y);

                            wallsList.Add(new Wall(wallId, p1, p2));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Invalid XML structure: missing 'GameSettings' element.");
                }
            }
            catch
            {
                // Handle exceptions related to file operations
                throw new FileNotFoundException("No file were found");
            }
        }

        // Default constructor
        public GameSettings()
        {
            wallsList = new List<Wall>();
        }
    }

}

