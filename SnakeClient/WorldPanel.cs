using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using SnakeGame.TheMap;
using System.Security.Cryptography;
using Microsoft.Maui.Graphics;

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
* Description: World panel to draw things on
 */
public class WorldPanel : IDrawable
{

    public delegate void ObjectDrawer(object o, ICanvas canvas);
    private IImage wall;
    private IImage background;
    private IImage gameOver;
    private GraphicsView graphicsView = new();
    private World theWorld;
    public int viewSize = 900;
    private float pastXValue = 0;
    private float pastYValue = 0;
    PlatformID platform = Environment.OSVersion.Platform;


    private bool initializedForDrawing = false;

    /// <summary>
    /// Loads an image from embedded resources
    /// </summary>
    /// <param name="name">The name of the image file to be loaded</param>
    /// <returns>An IImage object representing the loaded image</returns>
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeClient.Resources.Images";
        using (Stream stream = assembly.GetManifestResourceStream($"{path}.{name}"))
        {
#if MACCATALYST
            return PlatformImage.FromStream(stream);
#else
        return new W2DImageLoadingService().FromStream(stream);
#endif
        }
    }

    /// <summary>
    /// Initializes a new instance of the WorldPanel class
    /// </summary>
    public WorldPanel()
    {
        graphicsView.Drawable = this;
        graphicsView.HeightRequest = viewSize;
        graphicsView.WidthRequest = viewSize;
        graphicsView.BackgroundColor = Colors.Black;
    }

    /// <summary>
    /// Sets the current world context for this instance
    /// </summary>
    /// <param name="w">The world object to be set</param>
    public void SetWorld(World w)
    {
        theWorld = w;
    }

    /// <summary>
    /// Invalidates the current graphical view, forcing a redraw
    /// </summary>
    public void Invalidate()
    {
        graphicsView.Invalidate();
    }

    /// <summary>
    /// Initializes resources required for drawing
    /// </summary>
    private void InitializeDrawing()
    {
        wall = loadImage("wallsprite.png");
        background = loadImage("background.png");
        gameOver = loadImage("snakegameover.png");
        initializedForDrawing = true;
    }

    /// <summary>
    /// Retrieves the X-coordinate of the player
    /// </summary>
    /// <returns>The X-coordinate of the last segment of the player's snake</returns>
    private float GetPlayerX()
    {
        theWorld.SnakesList.TryGetValue(theWorld.SnakeID, out Snake snake);
        List<Vector2D> bodyList = snake.body;
        float returnVal = (float)bodyList[bodyList.Count - 1].GetX();
        return returnVal;
    }

    /// <summary>
    /// Retrieves the Y-coordinate of the player
    /// </summary>
    /// <returns>The Y-coordinate of the last segment of the player's snake</returns>
    private float GetPlayerY()
    {
        theWorld.SnakesList.TryGetValue(theWorld.SnakeID, out Snake snake);
        List<Vector2D> bodyList = snake.body;
        float returnVal = (float)bodyList[bodyList.Count - 1].GetY();
        return returnVal;
    }


    /// <summary>
    /// Transforms the canvas for Windows 
    /// </summary>
    /// <param name="canvas">The canvas to be transformed</param>
    public void TransformWindows(ICanvas canvas)
    {
        float playerXCurrent = GetPlayerX();
        float playerYCurrent = GetPlayerY();
        if (playerXCurrent < pastXValue) //this means the snake is moving left
        {
            float amountToShiftBy = pastXValue - playerXCurrent;
            canvas.Translate(amountToShiftBy, 0);
        }
        else if (playerYCurrent > pastYValue) //this means the snake is moving down
        {
            float amountToShiftBy = pastYValue - playerYCurrent;
            canvas.Translate(0, amountToShiftBy);
        }
        else if (playerXCurrent > pastXValue) //this means the snake is moving right
        {
            float amountToShiftBy = pastXValue - playerXCurrent;
            canvas.Translate(amountToShiftBy, 0);
        }
        else if (playerYCurrent < pastYValue) //this means the snake is moving up
        {
            float amountToShiftBy = pastYValue - playerYCurrent;
            canvas.Translate(0, amountToShiftBy);
        }

        pastXValue = GetPlayerX();
        pastYValue = GetPlayerY();
    }

    /// <summary>
    /// Transforms the canvas for Mac
    /// </summary>
    /// <param name="canvas">The canvas to be transformed</param>
    public void TransformMacUnix(ICanvas canvas)
    {
        canvas.Translate(-GetPlayerX() + (viewSize / 2), -GetPlayerY() + (viewSize / 2));
    }

    /// <summary>
    /// This runs whenever the drawing panel is invalidated and draws the game
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // We have to wait until Draw is called at least once
        // before loading the images
        if (!initializedForDrawing)
        {
            InitializeDrawing();
            initializedForDrawing = true;
        }
        lock (theWorld)
        {
            theWorld.SnakesList.TryGetValue(theWorld.SnakeID, out Snake snake);

        if (snake != null)
        {
            if (snake.alive == false)
            {

                canvas.ResetState();

                pastXValue = 0f;
                pastYValue = 0f;
            }
        }

            if (pastXValue == 0 && pastYValue == 0 && theWorld.SnakesList.Count > 0) //first time frame setup
        {
            if (snake.alive == false) //this check makes it override everything else and draws a game over screen
            {
                DrawDeath(canvas, snake);
                return; //in other words, if the snake is dead, draw nothing more 
            }
            if (snake.alive == true)
            {
                //this sets the camera to where the snake spawns to that it is centered again.
                pastXValue = GetPlayerX();
                pastYValue = GetPlayerY();
                canvas.Translate(-pastXValue + viewSize / 2, -pastYValue + viewSize / 2);
            }

        }



            // center the view on the player
            if (theWorld.SnakesList.Count > 0) //only fires if client is connected
            {
                //we found a workaround to add mac support by utilizing a custom transform method for each operating system. Tested to be working. 
                if (platform == PlatformID.Win32NT || platform == PlatformID.Win32Windows)
                {
                    TransformWindows(canvas);
                    if (theWorld.WorldSize != 0)
                    {
                        DrawObjectWithTransform(canvas, background, 0, 0, 0, BGDrawer);

                    }
                }
                else if (platform == PlatformID.Unix)
                {
                    TransformMacUnix(canvas);
                    canvas.DrawImage(background, (-theWorld.WorldSize / 2), (-theWorld.WorldSize / 2), theWorld.WorldSize, theWorld.WorldSize);
                }
                //Draw the walls of the world. 
                foreach (var p in theWorld.WallsList.Values)
                    DrawObjectWithTransform(canvas, p,
                      p.p1.GetX(), p.p1.GetY(), 0,
                      WallDrawer);

                //Draw the powerups(food) of the world. 
                int counter = 0;
                while (counter < theWorld.PowerupsList.Count)
                {
                    theWorld.PowerupsList.TryGetValue(counter, out Powerup p);
                    if (p != null && !p.died)
                    {
                        DrawObjectWithTransform(canvas, p,
                        p.loc.X, p.loc.Y, 0,
                        PowerupDrawer);
                    }
                    counter++;
                }

                //Draw the snakes(players) of the world. 
                foreach (var p in theWorld.SnakesList.Values)
                    if (p.died == false)
                    {
                        DrawObjectWithTransform(canvas, p,
                      p.dir.X, p.dir.Y, 0,
                      SnakeDrawer);
                    }
            }
        }
    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }
    /// <summary>
    /// 
    /// Draws the background of the map while shifting position accordingly to the snakes position.
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void BGDrawer(object o, ICanvas canvas)
    {
        canvas.DrawImage(background, -1000, -1000, 2000, 2000);
        //draw the scoreboard
        theWorld.SnakesList.TryGetValue(theWorld.SnakeID, out Snake snake);
        canvas.DrawRectangle((float)snake.body.Last<Vector2D>().GetX() + 350, (float)snake.body.Last<Vector2D>().GetY() - 450, 100, 150);
        int counter = 0;
        int snakeRank = 4;
        while (counter <= 100)
        {
            HorizontalAlignment textPosition = new HorizontalAlignment();
            canvas.FontSize = 10;
            canvas.FontColor = Color.FromRgb(0, 0, 0);
            if (theWorld.LeaderBoard[snakeRank] != null) 
            canvas.DrawString(theWorld.LeaderBoard[snakeRank].name + ": " + theWorld.LeaderBoard[snakeRank].score, (float)snake.body.Last<Vector2D>().GetX() + 353, (float)snake.body.Last<Vector2D>().GetY() - 430 + counter, textPosition);
            counter = counter + 25;
            snakeRank--;
        }

    }


    /// <summary>
    /// Draws the game over screen and displays the player's length at death along with the top length in a historical score list (extra feature).
    /// </summary>
    /// <param name="canvas">The canvas on which to draw</param>
    /// <param name="snake">The snake object used to calculate the length at death</param>
    private void DrawDeath(ICanvas canvas, Snake snake)
    {
        double score = 0;
        int counter = 0;
        // Calculate the length of the snake at death
        while (snake.body.Count > counter + 1)
        {
            if (snake.body[counter].GetX() == snake.body[counter + 1].GetX()) //if segment is vertical
            {
                double distance = Math.Abs(snake.body[counter].GetY() - snake.body[counter + 1].GetY());
                if (distance!>theWorld.Size)
                score = score + distance;
            }
            else if (snake.body[counter].GetY() == snake.body[counter + 1].GetY()) //if segment is horizontal
            {
                double distance = Math.Abs(snake.body[counter].GetX() - snake.body[counter + 1].GetX());
                if (distance! > theWorld.Size)
                score = score + distance;
            }

            counter++;
        }
        // Draw the game over screen
        canvas.FillColor = Color.FromRgb(0, 0, 0);
        canvas.FillRectangle(0, 0, viewSize, viewSize);
        canvas.DrawImage(gameOver, 0, 0, gameOver.Width + 250, gameOver.Height + 250);
        canvas.FillColor = Color.FromRgb(255, 0, 0);
        score = (int)score / 5;
        if (!theWorld.scores.Contains((int)score)) { theWorld.scores.Add((int)score); } // Add the score to the saved scores if not already saved
        canvas.FontSize = 20;
        canvas.FontColor = Color.FromRgb(255, 0, 0);
        int verticalPosition = 50;
        canvas.FontColor = Color.FromRgb(200, 200, 200);
        canvas.DrawString("Length: ", 0, 0, 885, 435, HorizontalAlignment.Center, VerticalAlignment.Center);
        theWorld.scores.Sort();
        theWorld.scores.Reverse();
        int numberOfScores = 0;
        foreach (int scoreHistory in theWorld.scores)
        {
            canvas.DrawString("" + scoreHistory, 0, 0, 885, 435 + verticalPosition, HorizontalAlignment.Center, VerticalAlignment.Center);
            verticalPosition = verticalPosition + 50;
            numberOfScores++;
            if (numberOfScores == 5) { break; } // Limit to top 5 scores
        }
    }


    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate.
    /// This method works by first selecting a color to draw the snake in. as well as drawing the nametags of the snake above their heads. 
    /// Followed by a loop that takes the snake.body array, and treats it like the Wall input where each index is a point in (x,y). it then can fill in the gaps
    /// between that point and the NEXT point in the index. 
    /// </summary>
    /// <param name="o">The player to draw</param>
    /// <param name="canvas"></param>
    private void SnakeDrawer(object o, ICanvas canvas)
    {
        Snake snake = o as Snake;


        canvas.FillColor = Color.FromRgb(snake.ColorCode.Item1,
            snake.ColorCode.Item2, snake.ColorCode.Item3);
            if (snake.alive == false) { canvas.FillColor = Color.FromRgb(25, 25, 25); }

        //draw the names of the snakes. 
        HorizontalAlignment textPosition = new HorizontalAlignment();
        canvas.DrawString(snake.name, (float)snake.body.Last<Vector2D>().GetX() - 18, (float)snake.body.Last<Vector2D>().GetY() - 20, textPosition);


        int counter = 0;
        while (snake.body.Count > counter + 1)
        {
            Vector2D startOfSegment = snake.body[counter];
            Vector2D endOfSegment = snake.body[counter + 1];

            //draw the head circle
            canvas.FillCircle((float)snake.body.Last<Vector2D>().GetX(), (float)snake.body.Last<Vector2D>().GetY(), 4);
            //draw the last circle
            canvas.FillCircle((float)snake.body.First<Vector2D>().GetX(), (float)snake.body.First<Vector2D>().GetY(), 4);

            if (startOfSegment.X == endOfSegment.X) //vertical snake segment
            {
                if (startOfSegment.Y < endOfSegment.Y)
                {
                    //draw from bottom to top
                    double Ysegment = startOfSegment.Y;
                    if (Math.Abs(endOfSegment.Y - startOfSegment.Y) < (theWorld.Size - 10))
                    while (Ysegment < endOfSegment.Y)
                    {
                        canvas.FillRectangle((float)startOfSegment.X - 4, (float)Ysegment - 4, 8, 8);
                        Ysegment = Ysegment + 5;
                    }

                }
                if (startOfSegment.Y > endOfSegment.Y)
                {
                    //draw from top to bottom
                    double Ysegment = startOfSegment.Y;
                    if (Math.Abs(endOfSegment.Y - startOfSegment.Y) < (theWorld.Size - 10))
                        while (Ysegment > endOfSegment.Y)
                    {
                        canvas.FillRectangle((float)startOfSegment.X - 4, (float)Ysegment - 4, 8, 8);
                        Ysegment = Ysegment - 5;
                    }
                    
                }
            }
            if (startOfSegment.Y == endOfSegment.Y)//horizontal snake segment
            {
                if (startOfSegment.X < endOfSegment.X)
                {
                    //draw from left to right
                    double Xsegment = startOfSegment.X;
                    if (Math.Abs(endOfSegment.X - startOfSegment.X) < (theWorld.Size - 10))
                        while (Xsegment < endOfSegment.X)
                    {
                        canvas.FillRectangle((float)Xsegment - 4, (float)startOfSegment.Y - 4, 8, 8);
                        Xsegment = Xsegment + 5;
                    }

                }
                if (startOfSegment.X > endOfSegment.X)
                {
                    
                    double Xsegment = startOfSegment.X;
                    if (Math.Abs(endOfSegment.X - startOfSegment.X) < (theWorld.Size - 10))
                        while (Xsegment > endOfSegment.X)
                    {

                        canvas.FillRectangle((float)Xsegment - 4, (float)startOfSegment.Y - 4, 8, 8);
                        Xsegment = Xsegment - 5;
                    }
                }
            }
            counter++;
        }


    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// This method draws the food of the world as one of two colors. 
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 10;
        if (p.power % 2 == 0)
            canvas.FillColor = Colors.Orange;
        else
            canvas.FillColor = Colors.Green;

        // Ellipses are drawn starting from the top-left corner.
        // So if we want the circle centered on the powerup's location, we have to offset it
        // by half its size to the left (-width/2) and up (-height/2)
        canvas.FillEllipse(-(width / 2), -(width / 2), width + 5, width + 5);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// This method draws the walls of the world by taking the P1 as a starting X,Y point, and a P2 as a second point as and endpoint. 
    /// it then fills inbetween those points with walls.png's
    /// </summary>
    /// <param name="o">The walls to draw</param>
    /// <param name="canvas"></param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        float width = this.wall.Width;
        float height = this.wall.Height;

        //okay, so 0,0 for whatever reason is the center of the canvas. and its also an accurate location for a wall cause the hitboxes match. 
        // so we just need to normalize each p1 of each wall to 0,0 and draw from there.

        Wall wall = o as Wall;
        Vector2D startPoint = new Vector2D(wall.p1.X, wall.p1.Y);
        Vector2D endPoint = new Vector2D(wall.p2.X, wall.p2.Y);

        //normalize the common P1 coordinate to 0

        if (endPoint.Y == startPoint.Y) //if y's are the same
        {
            endPoint.Y = 0;
            startPoint.Y = 0;
        }
        else if (endPoint.X == startPoint.X) //if x's are the same
        {
            endPoint.X = 0;
            startPoint.X = 0;
        }

        //now, whatever the commonality is, will be at the 0 line of that axis.
        //now to shift the non common coordinate.
        //first, find which is different.
        if (endPoint.Y != startPoint.Y) //if the Y's are different
        {
            endPoint.Y = endPoint.Y - startPoint.Y; startPoint.Y = 0;
        }
        else if (endPoint.X != startPoint.X)//if the X's are different.
        {
            endPoint.X = endPoint.X - startPoint.X; startPoint.X = 0;
        }


        //now, fill in the walls between the endpoints.
        while (startPoint.X < endPoint.X) //the end is further right. draw from left to right.
        {
            canvas.DrawImage(this.wall, (float)startPoint.X - width / 2, (float)startPoint.Y - height / 2, width, height);
            startPoint.X = startPoint.X + 25; //5 is smallest interval due to coords server sends out being %5=0
        }
        while (startPoint.X > endPoint.X) //the end is further left. draw from right to left. 
        {
            canvas.DrawImage(this.wall, (float)startPoint.X - width / 2, (float)startPoint.Y - height / 2, width, height);
            startPoint.X = startPoint.X - 25;
        }
        while (startPoint.Y < endPoint.Y) //the end is further down, draw top to bottom
        {
            canvas.DrawImage(this.wall, (float)startPoint.X - width / 2, (float)startPoint.Y - height / 2, width, height);
            startPoint.Y = startPoint.Y + 25; //5 is smallest interval due to coords server sends out being %5=0
        }
        while (startPoint.Y > endPoint.Y) //the end is further up, draw bottom to top.
        {
            canvas.DrawImage(this.wall, (float)startPoint.X - width / 2, (float)startPoint.Y - height / 2, width, height);
            startPoint.Y = startPoint.Y - 25;
        }
        canvas.DrawImage(this.wall, (float)endPoint.X - width / 2, (float)endPoint.Y - height / 2, width, height); //this is the endpoint. just need to fill in between
    }

}
