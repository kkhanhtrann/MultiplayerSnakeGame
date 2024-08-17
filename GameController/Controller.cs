using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NetworkUtil;
using SnakeGame;
using SnakeGame.TheMap;
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
* Description: Controller which takes care of all of the logic
 */
namespace GameController
{
    public class Controller
    {
        private bool receivedSnakeInfo = false;
        private bool WallNeedUpdates = false;
        private bool SnakeNeedUpdates = false;
        private bool PowerNeedUpdates = false;

        //Dictionary<int, Snake> SnakesList = new Dictionary<int, Snake>();
        // Dictionary<int, Wall> WallsList = new Dictionary<int, Wall>();
        // Dictionary<int, Powerup> PowerupsList = new Dictionary<int, Powerup>();
        private World theWorld;

        // Event that can be triggered when messages arrive in which the walls need to be updated
        public event Action? CallWallUpdate;

        // Event that can be triggered when messages arrive in which the snakes need to be updated
        public event Action? CallSnakeUpdate;

        // Event that can be triggered when messages arrive in which the powerups need to be updated
        public event Action? CallPowerUpdate;

        // Event that can be triggered when a connection is established Subscribers to this event should match the ConnectedHandler delegate signature
        public event Action? Connected;

        // Event that can be triggered in case of an error Subscribers to this event should match the ErrorHandler delegate signature
        public event Action<string>? Error;

        SocketState? theServer = null;
        public Controller()
        {
            theWorld = new World(2000);
        }
        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <param name="addr"></param>
        public void Connect(string addr)
        {
            Networking.ConnectToServer(OnConnect, addr, 11000);
        }

        public World GetWorld()
        {
            return theWorld;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Error connecting");
                return;
            }
            theServer = state;
            // Start an event loop to receive messages from the server 
            state.OnNetworkAction = ReceiveMessage;

            // inform the view
            Connected?.Invoke();
            Networking.GetData(state);
        }

        /// <summary>
        /// Receive Message from the server
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveMessage(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // inform the view
                Error?.Invoke("Lost connection to server");
                return;
            }
            GetAllMessages(state);

            // Continue the event loop
            Networking.GetData(state);
        }

        /// <summary>
        /// Proccess the data
        /// </summary>
        /// <param name="state"></param>
        private void GetAllMessages(SocketState state)
        {
            string totalData = state.GetData();
            Console.WriteLine(totalData);
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");


            // Loop until we have processed all messages.
            // We may have received more than one.

            List<string> serverMessage = new List<string>();
            lock (theWorld)
            {


                foreach (string p in parts)
                {
                    // Ignore empty strings added by the regex splitter
                    if (p.Length == 0)
                        continue;
                    // The regex splitter will include the last string even if it doesn't end with a '\n',
                    // So we ignore it if this happens. 
                    if (p[p.Length - 1] != '\n')
                        break;

                    // build a list of messages to send to the view
                    serverMessage.Add(p);

                    // Then remove it from the SocketStßate's growable buffer
                    state.RemoveData(0, p.Length);
                }

                if (!receivedSnakeInfo)
                {
                    // Extracting SnakeID and WorldSize from the server message
                    theWorld.SnakeID = int.Parse(serverMessage[0]);
                    theWorld.WorldSize = int.Parse(serverMessage[1]);
                    serverMessage.RemoveRange(0, 2);
                    receivedSnakeInfo = true;
                    // Analyzing each remaining JSON string in the server message

                    foreach (string p in serverMessage)
                    {
                        AnalyzeJson(p);
                    }

                }
                // Analyzing each JSON string in the server message
                else foreach (string p in serverMessage)
                    {
                        AnalyzeJson(p);
                    }

                // inform the view
                if (SnakeNeedUpdates)
                {
                    LeaderBoardUpdate(theWorld.SnakesList);
                    CallSnakeUpdate?.Invoke();
                    SnakeNeedUpdates = false;
                }
                if (PowerNeedUpdates)
                {
                    CallPowerUpdate?.Invoke();
                    PowerNeedUpdates = false;
                }
                if (WallNeedUpdates)
                {
                    CallWallUpdate?.Invoke();
                    WallNeedUpdates = false;
                }
            }
        }

        /// <summary>
        /// Analyzes a JSON string to update or add new elements to the lists based on the type of object (Snake, Powerup, Wall) 
        /// </summary>
        /// <param name="data">The JSON string to be analyzed</param>
        private void AnalyzeJson(string data)
        {
            // Parse the JSON string into a JsonDocument
            JsonDocument doc = JsonDocument.Parse(data);

            // Access the root element of the JSON document
            JsonElement rootVal = doc.RootElement;

            // Check if the root element has a "snake" property
            if (rootVal.TryGetProperty("snake", out JsonElement snakeID))
            {
                // Deserialize the JSON data into a Snake object
                Snake JsonSnake = JsonSerializer.Deserialize<Snake>(data)!;

                // Check if the snake ID already exists in SnakesList
                if (!theWorld.SnakesList.ContainsKey(snakeID.GetInt32()))
                {
                    // If it doesn't exist, add the new Snake object to SnakesList
                    theWorld.SnakesList.Add(snakeID.GetInt32(), JsonSnake);
                    SnakeColor(theWorld.SnakesList[snakeID.GetInt32()]);
                    SnakeNeedUpdates = true;
                }
                else
                {
                    // If it exists, update the existing Snake object
                    SnakeUpdate(theWorld.SnakesList[snakeID.GetInt32()], JsonSnake);
                }
            }
            // Check if the root element has a "power" property
            else if (rootVal.TryGetProperty("power", out JsonElement powerID))
            {
                // Deserialize the JSON data into a Powerup object
                Powerup JsonPower = JsonSerializer.Deserialize<Powerup>(data)!;

                // Check if the power ID already exists in PowerupsList
                if (!theWorld.PowerupsList.ContainsKey(powerID.GetInt32()))
                {
                    // If it doesn't exist, add the new Powerup object to PowerupsList
                    theWorld.PowerupsList.Add(powerID.GetInt32(), JsonPower);
                    PowerNeedUpdates = true;
                }
                else
                {
                    // If it exists, update the existing Powerup object
                    PowerUpdate(theWorld.PowerupsList[powerID.GetInt32()], JsonPower);
                }
            }
            // Check if the root element has a "wall" property
            else if (rootVal.TryGetProperty("wall", out JsonElement wallID))
            {
                // Deserialize the JSON data into a Wall object
                Wall JsonWall = JsonSerializer.Deserialize<Wall>(data)!;

                // Check if the wall ID already exists in WallsList
                if (!theWorld.WallsList.ContainsKey(wallID.GetInt32()))
                {
                    // If it doesn't exist, add the new Wall object to WallsList
                    theWorld.WallsList.Add(wallID.GetInt32(), JsonWall);
                    WallNeedUpdates = true;
                }
                else
                {
                    // If it exists, update the existing Wall object
                    WallUpdate(theWorld.WallsList[wallID.GetInt32()], JsonWall);
                }
            }

        }

        /// <summary>
        /// Updates the properties of the current wall
        /// </summary>
        /// <param name="currentWall">The current wall instance to be updated</param>
        /// <param name="jsonWall">The wall instance obtained from JSON </param>
        private void WallUpdate(Wall currentWall, Wall jsonWall)
        {
            currentWall.p1 = jsonWall.p1; // Update first point of the wall
            currentWall.p2 = jsonWall.p2; // Update second point of the wall
            WallNeedUpdates = true;
        }

        /// <summary>
        /// Updates the properties of the current powerup
        /// </summary>
        /// <param name="currentWall">The current powerup instance to be updated</param>
        /// <param name="jsonWall">The powerup instance obtained from JSON </param>
        private void PowerUpdate(Powerup currrentPowerup, Powerup jsonPower)
        {
            currrentPowerup.loc = jsonPower.loc; // Update location of the powerup
            currrentPowerup.died = jsonPower.died; // Update died status of the powerup
            PowerNeedUpdates = true;
        }

        /// <summary>
        /// Updates the properties of the current snake 
        /// </summary>
        /// <param name="currentSnake">The current snake instance to be updated</param>
        /// <param name="JsonSnake">The snake instance obtained from JSON data</param>
        private void SnakeUpdate(Snake currentSnake, Snake JsonSnake)
        {
            currentSnake.body = JsonSnake.body;  // Update body
            currentSnake.dir = JsonSnake.dir;    // Update direction
            currentSnake.score = JsonSnake.score; // Update score
            currentSnake.died = JsonSnake.died;  // Update died status
            currentSnake.alive = JsonSnake.alive; // Update alive status
            currentSnake.dc = JsonSnake.dc;      // Update disconnect status
            currentSnake.join = JsonSnake.join;  // Update join status
            if (currentSnake.dc)
                theWorld.SnakesList.Remove(currentSnake.snake);
            SnakeNeedUpdates = true;
        }

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="message"> Message to be send </param>
        public void MessageEntered(string message)
        {
            if (theServer != null)
                Networking.Send(theServer.TheSocket, message + "\n");
            else Error?.Invoke("Disconnected from the Server");
        }

        /// <summary>
        /// Updates the leaderboard with the top scoring snakes
        /// </summary>
        /// <param name="snakeList">A dictionary of all snakes</param>
        private void LeaderBoardUpdate(Dictionary<int, Snake> snakeList)
        {
            // Convert the dictionary values to a list for sorting
            List<Snake> SnakeList = new List<Snake>(snakeList.Values);

            // Sort the list of snakes based on their scores
            SnakeList.Sort((snakeA, snakeB) =>
            {
                // Comparison is done based on snake scores (ascending order)
                return snakeA.score.CompareTo(snakeB.score);
            });

            // Initialize the new leaderboard 
            theWorld.LeaderBoard = new Snake[5];

            // Variable to track the index in the leaderboard array
            int index = 0;

            // Iterate through the sorted list of snakes
            foreach (Snake snake in SnakeList)
            {
                // Update the leaderboard with the current snake
                theWorld.LeaderBoard[index] = snake;
                index++;

                // Break the loop if the leaderboard is filled
                if (index >= theWorld.LeaderBoard.Length) break;
            }
        }



        /// <summary>
        /// Sets the color of the snake based on the number of snakes in the game
        /// </summary>
        /// <param name="snake">The snake object whose color needs to be set</param>
        private void SnakeColor(Snake snake)
        {
            switch (theWorld.SnakesList.Count)
            {
                case 1:
                    snake.ColorCode = new Tuple<int, int, int>(0, 0, 255); // Blue
                    break;
                case 2:
                    snake.ColorCode = new Tuple<int, int, int>(0, 255, 0); // Green
                    break;
                case 3:
                    snake.ColorCode = new Tuple<int, int, int>(255, 255, 0);// Yellow
                    break;
                case 4:
                    snake.ColorCode = new Tuple<int, int, int>(100, 64, 0); // Orange
                    break;
                case 5:
                    snake.ColorCode = new Tuple<int, int, int>(139, 69, 19); // Brown
                    break;
                case 6:
                    snake.ColorCode = new Tuple<int, int, int>(255, 20, 147); // Pink
                    break;
                case 7:
                    snake.ColorCode = new Tuple<int, int, int>(255, 255, 255); // White
                    break;
                case 8:
                    snake.ColorCode = new Tuple<int, int, int>(255, 0, 0); // Red
                    break;
            }
        }
    }
}