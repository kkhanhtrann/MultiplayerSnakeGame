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
* Description: Platform to show the views
 */
using System.Collections.Generic;
using System.Text;
using GameController;

public partial class MainPage : ContentPage
{
    private Controller controller = new Controller();
    public MainPage()
    {
        // Initialize page components
        InitializeComponent();
        //graphicsView.Invalidate();
        worldPanel.SetWorld(controller.GetWorld());

        // Subscribe to controller events
        controller.Connected += HandleConnected;
        controller.CallSnakeUpdate += UpdateSnake; //Update snakes
        controller.CallPowerUpdate += UpdatePowerup; //Update powerups
        controller.CallWallUpdate += UpdateWall; // Update walls
        controller.Error += NetworkErrorHandler;
    }

    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        string text = entry.Text.ToLower();
        if (text == "w")
        {
            Console.WriteLine("w");
            controller.MessageEntered("{\"moving\":\"up\"}");
            // Move up
        }
        else if (text == "a")
        {
            Console.WriteLine("a");
            controller.MessageEntered("{\"moving\":\"left\"}");
            // Move left
        }
        else if (text == "s")
        {
            Console.WriteLine("s");
            controller.MessageEntered("{\"moving\":\"down\"}");
            // Move down
        }
        else if (text == "d")
        {
            Console.WriteLine("d");
            controller.MessageEntered("{\"moving\":\"right\"}");
            // Move right
        }
        entry.Text = "";
    }




    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }

        // Disable the controls and try to connect
        connectButton.IsEnabled = false;
        serverText.IsEnabled = false;
        controller.Connect(serverText.Text);

        keyboardHack.Focus();
    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by ...\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }

    ////////////////////////////////////////////////////////////
    //====================METHOD FOR EVENT====================//
    ////////////////////////////////////////////////////////////


    //private void NetworkErrorHandler()
    //{
    //    DisplayAlert("Error", "Disconnected from server", "OK");
    //}

    /// <summary>
    /// Handler for the controller's Error event
    /// </summary>
    /// <param name="err"></param>
    private void NetworkErrorHandler(string err)
    {
        // Show the error
        Dispatcher.Dispatch(() => DisplayAlert("Error", err, "OK"));

        // Then re-enable the controlls so the user can reconnect
        Dispatcher.Dispatch(
          () =>
          {
              connectButton.IsEnabled = true;
              serverText.IsEnabled = true;
          });
    }


    /// <summary>
    /// Updating the snakes on the screen
    /// </summary>
    /// <param name="newMessages">A collection of new messages to display</param>
    private void UpdateSnake()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Updating the walls on the screen
    /// </summary>
    /// <param name="newMessages">A collection of new messages to display</param>
    private void UpdateWall()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Updating the powerups on the screen
    /// </summary>
    /// <param name="newMessages">A collection of new messages to display</param>
    private void UpdatePowerup()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }


    /// <summary>
    /// Handles the connected event by dispatching a UI update and sending a message to the server
    /// </summary>
    private void HandleConnected()
    {
        Dispatcher.Dispatch(() =>
        {
            string message = nameText.Text;
            // Reset the textbox
            nameText.Text = "";
            // Send the message to the server
            controller.MessageEntered(message);
        });
    }
}