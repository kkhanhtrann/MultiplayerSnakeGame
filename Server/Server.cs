using System.Collections;
using System.Diagnostics;
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
* Created date: 12/07/2023
*
* Description: Represents the game Server
 */
namespace Server;
class Server
{
    // A map of clients that are connected, each with an ID
    private static Dictionary<long, SocketState>? clients;
    // A map of client's ID each check if they received the world info
    private static Dictionary<long, bool>? ClientReveiveInfo;
    private static GameSettings? serverSetting;
    private static ServerController? serverState;

    static void Main(string[] args)
    {
        // serverState = new ServerController();
        Server server = new Server();
        server.StartServer();
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        //while (true) { if (stopwatch.ElapsedMilliseconds > 5000) { stopwatch.Restart(); break; } }
        while (true)
        {
            if (stopwatch.ElapsedMilliseconds >= serverSetting!.msPerFrame)
            {
                stopwatch.Restart();
                lock (clients!)
                    Server.serverState!.UpdateWorld();
                lock (clients!)
                    foreach (SocketState socketState in clients!.Values)
                    {
                        if (ClientReveiveInfo![socketState.ID] == true)
                            sendWorldTo(socketState);
                    }
            }
        }
    }
    /// <summary>
    /// Initialized the server's state
    /// </summary>
    public Server()
    {
        // Get the base directory usually it will be something like Server/bin/Debug/net7.0
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Navigate up three levels to the Server directory
        DirectoryInfo directoryInfo = new DirectoryInfo(baseDir);
        string serverDir = directoryInfo.Parent!.Parent!.Parent!.FullName!;

        serverSetting = new GameSettings(Path.Combine(serverDir, "settings.xml"));
        serverState = new ServerController();
        serverState.setting = serverSetting;
        serverState.getWorld().foodCount = serverSetting.maxPowerups;
        serverState.getWorld().speed = serverSetting.snakeSpeed;
        serverState.getWorld().shrinkSnake = serverSetting.shrinkSnake;
        serverState.BuildWorld(serverSetting.wallsList);
        clients = new Dictionary<long, SocketState>();
        ClientReveiveInfo = new Dictionary<long, bool>();
    }
    /// <summary>
    /// Sends the current world state to the connected client
    /// </summary>
    /// <param name="theSocket">The socket state representing the client connection</param>
    /// <remarks>
    /// The world state includes both the list of snakes and the list of powerups
    /// The data is sent as JSON strings converted from the respective list
    /// </remarks>
    private static void sendWorldTo(SocketState theSocket)
    {
        // Ensure the server state is not null before attempting to access the world
        if (serverState != null)
        {
            // Retrieve the world object from the server state
            World world = serverState.getWorld();

            // Convert the snakes list to a JSON string and send it to the client
            Networking.Send(theSocket.TheSocket, JsonConverter(world.SnakesList));

            // Convert the powerups list to a JSON string and send it to the client
            Networking.Send(theSocket.TheSocket, JsonConverter(world.PowerupsList));
        }
    }


    /// <summary>
    /// Start accepting Tcp sockets connections from clients
    /// </summary>
    public void StartServer()
    {
        // This begins an "event loop"
        Networking.StartServer(NewClientConnected, 11000);

        // Console.WriteLine("Server is running");
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a new client connects 
    /// </summary>
    /// <param name="state">The SocketState representing the new client</param>
    private void NewClientConnected(SocketState state)
    {
        if (state.ErrorOccurred)
            return;

        // Save the client state
        // Need to lock here because clients can disconnect at any time
        lock (clients!)
        {
            clients[state.ID] = state;
            ClientReveiveInfo![state.ID] = false;
        }


        // change the state's network action to the 
        // receive handler so we can process data when something
        // happens on the network
        state.OnNetworkAction = ReceiveMessage;


        Networking.GetData(state);
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a network action occurs
    /// </summary>
    /// <param name="state"></param>
    private void ReceiveMessage(SocketState state)
    {
        // Remove the client if they aren't still connected
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }
        lock (clients!)
        {
            ProcessMessage(state);
            // Continue the event loop that receives messages from this client
            Networking.GetData(state);
        }
    }


    /// <summary>
    /// Given the data that has arrived so far, 
    /// potentially from multiple receive operations, 
    /// determine if we have enough to make a complete message,
    /// and process it (print it and broadcast it to other clients).
    /// </summary>
    /// <param name="sender">The SocketState that represents the client</param>
    private void ProcessMessage(SocketState state)
    {
        string totalData = state.GetData();

        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        // Loop until we have processed all messages.
        // We may have received more than one.
        foreach (string p in parts)
        {
            // Ignore empty strings added by the regex splitter
            if (p.Length == 0)
                continue;
            // The regex splitter will include the last string even if it doesn't end with a '\n',
            // So we need to ignore it if this happens. 
            if (p[p.Length - 1] != '\n')
                break;

            string userSent = p.Substring(0, p.Length - 1);
            switch (userSent)
            {
                case "{\"moving\":\"up\"}":
                    serverState!.getWorld().changeSnakeDirection(state.ID, "up");
                    break;
                case "{\"moving\":\"down\"}":
                    serverState!.getWorld().changeSnakeDirection(state.ID, "down");

                    break;
                case "{\"moving\":\"left\"}":
                    serverState!.getWorld().changeSnakeDirection(state.ID, "left");

                    break;
                case "{\"moving\":\"right\"}":
                    serverState!.getWorld().changeSnakeDirection(state.ID, "right");

                    break;

            }
            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, p.Length);


            // Lock here beccause we can't have new connections 
            // adding while looping through the clients list.
            // We also need to remove any disconnected clients.
            HashSet<long> disconnectedClients = new HashSet<long>();
            lock (clients!)
            {
                if (!ClientReveiveInfo![state.ID])
                {
                    // Retrieve the Snake object associated with the client's ID and update its name
                    Snake snake = serverState!.validSnake((int)state.ID, p.Substring(0, p.Length - 1));

                    // Send initial game information to the client
                    if (!Networking.Send(state.TheSocket, state.ID + "\n" + serverSetting!.universeSize + "\n") ||
                        !Networking.Send(state.TheSocket, JsonConverter(serverSetting.wallsList)) ||
                        !Networking.Send(state.TheSocket, JsonConverter(snake)))
                    {
                        // If sending fails, remove the snake from the world and disconnect the client
                        serverState!.getWorld().SnakesList.Remove(snake.snake);
                        RemoveClient(state.ID);
                    }

                    // Set the flag indicating that the client has received initial information
                    ClientReveiveInfo[state.ID] = true;
                }

            }
            foreach (long id in disconnectedClients)
                RemoveClient(id);
        }
    }



    /// <summary>
    /// Removes a client from the clients dictionary
    /// </summary>
    /// <param name="id">The ID of the client</param>
    private void RemoveClient(long id)
    {
        //Console.WriteLine("Client " + id + " disconnected");
        lock (clients!)
        {
            clients.Remove(id);
            ClientReveiveInfo!.Remove(id);
        }
    }

    /// <summary>
    /// Converts objects to JSON strings
    /// </summary>
    /// <param name="element">The object to be converted to JSON</param>
    /// <returns>A JSON representation of the input object</returns>
    private static string JsonConverter(Object element)
    {
        StringBuilder returnString = new StringBuilder();

        // Check if the input element is a List of Walls
        if (element is List<Wall>)
        {
            // Iterate through each Wall in the list and serialize it to JSON
            lock (clients!)
                foreach (Wall wall in (List<Wall>)element)
                {
                    string deserialize = JsonSerializer.Serialize<Wall>(wall);
                    returnString.Append(deserialize + " \n");
                }
        }
        // Check if the input element is a Dictionary of Snakes
        else if (element is Dictionary<int, Snake>)
        {
            // Iterate through each Snake in the dictionary's values and serialize it to JSON
            lock (clients!)

                foreach (Snake snake in ((Dictionary<int, Snake>)element).Values)
                {
                    string deserialize = JsonSerializer.Serialize<Snake>(snake);
                    returnString.Append(deserialize + " \n");
                }
        }
        // Check if the input element is a Dictionary of Powerups
        else if (element is Dictionary<int, Powerup>)
        {
            // Iterate through each Powerup in the dictionary's values and serialize it to JSON
            lock (clients!)

                foreach (Powerup powerup in ((Dictionary<int, Powerup>)element).Values)
                {
                    string deserialize = JsonSerializer.Serialize<Powerup>(powerup);
                    returnString.Append(deserialize + " \n");
                }
        }
        // Check if the input element is a single Snake object
        else if (element is Snake)
        {
            // Serialize the single Snake object to JSON
            string deserialize = JsonSerializer.Serialize<Snake>((Snake)element);
            returnString.Append(deserialize + " \n");
        }
        // Check if the input element is a single Powerup object
        else if (element is Powerup)
        {
            // Serialize the single Powerup object to JSON
            string deserialize = JsonSerializer.Serialize<Powerup>((Powerup)element);
            returnString.Append(deserialize + " \n");
        }

        // Convert the StringBuilder to a string and return it
        return returnString.ToString();
    }
}

