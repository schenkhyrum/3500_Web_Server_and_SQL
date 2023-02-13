/// <summary> 
/// Author:    Samuel Hancock 
/// Partner:   Hyrum Schenk 
/// Date:      April 1, 2020 
/// Course:    CS 3500, University of Utah, School of Computing 
/// Copyright: CS 3500, Samuel Hancock and Hyrum Schenk - This work may not be copied for use in Academic Coursework. 
/// 
/// I, Samuel Hancock and Hyrum Schenk, certify that I wrote this code from scratch and did not copy it in part or whole from  
/// another source.  All references used in the completion of the assignment are cited in my README file. 
/// 
/// File Contents 
/// 
///    File contains code for an Agario game client that will communicate with an Agario server.
///    The server recieves circles from the client and displays the circles on a Windows Form.
///    This client sends the mouse movement to the server which translates recieved data into
///    circle movement. The client also handles when other circles are eaten by others.
/// </summary>

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using Model;
using NetworkingNS;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Numerics;

namespace ViewController
{
    public partial class AgarioClientView : Form
    {

        List<Circle> CirclesToAdd = new List<Circle>();

        List<Circle> ToRemove = new List<Circle>();

        World world = new World(null);

        private Preserved_Socket_State server;

        private Circle playerCircle;


        ILogger logger;//How are we going to log?

        /// <summary>
        /// Initializes the windows form
        /// </summary>
        public AgarioClientView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handler for the connect button in Agario Client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Connect(object sender, EventArgs eventArgs)
        {
            if (this.server != null && this.server.socket.Connected)
            {
                Debug.WriteLine("Shutting down the connection.");
                this.server.socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                return;
            }

            if(Player_Name_Textbox.TextLength == 0)
            {
                MessageBox.Show("Cannot have an empty name, please enter a name and try again.");
                return;
            }


            Debug.WriteLine("Asking networking code to connect to server.");
            //Need an error handler to catch when an invalid host name is supplied
            this.server = Networking.Connect_to_Server(Contact_Established, $"{IPAddress_Textbox.Text}");
        }

        /// <summary>
        /// This method assigns the data received handler and gets the 
        /// </summary>
        /// <param name="obj">Preserved Socket State from Networking</param>
        private void Contact_Established(Preserved_Socket_State obj)
        {
            try
            {
                Debug.WriteLine("Contact Established");

                obj.on_data_received_handler = GetPlayer;
                Networking.Send(obj.socket, $"{Player_Name_Textbox.Text}\n");

                Networking.await_more_data(obj);
            }
            catch (Exception)
            {
                MessageBox.Show("Could not connect to server, try a different IP Address or server name");
                return;
            }
        }

        /// <summary>
        /// Hander for recieving the player information from the server and calls method to prepare Windows form for game
        /// </summary>
        /// <param name="obj"> Preserved Socket State from Networking </param>
        private void GetPlayer(Preserved_Socket_State obj)
        {
            Invalidate();

            //Calls method to recieve food and other player locations from server
            obj.on_data_received_handler = TheRestOfTheCircle;

            try
            {
                //Recieve player from server
                Circle player = JsonConvert.DeserializeObject<Circle>(obj.Message);

                world.SetPlayerId(player.ID);//To keep track of which circle is our player

                world.AddCircle(player);
                Networking.await_more_data(obj);

                //Prepare Windows Form
                Invoke(new MethodInvoker(() => PrepareGUI(player)));
            }
            catch
            {
                Debug.WriteLine($"Message from server was not a circle or player. Message was \n {obj.Message}");
            }
        }

        /// <summary>
        /// This method clears labels and textboxes in the way of where the game will be displayed
        /// </summary>
        /// <param name="player"></param>
        private void PrepareGUI(Circle player)
        {
            //world.AddCircle(player);
            this.playerCircle = player;
            Recieved_Player_Name_Label.Text = player.name;
            this.Recieved_Player_ID_Label.Text = player.ID.ToString();
            this.Connect_Button.Hide();
            this.Player_Name_Label.Hide();
            this.Player_Name_Textbox.Hide();
            this.IPAddress_Label.Hide();
            this.IPAddress_Textbox.Hide();
            this.Paint += new PaintEventHandler(this.Draw_Game_View);
        }

        /// <summary>
        /// This method will continuously draw the game view as long as the server has information
        /// being sent to the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Draw_Game_View(object sender, PaintEventArgs e)
        {
            try
            {
                //Call method to send position to server
                Send_Position_To_Server();
                lock (world)
                {
                    //Initialize variables used in screen zoom calculaiton
                    int screenToRadiusRatio = 200;
                    Dictionary<int, Circle> allCircles = world.GetCircles();
                    SolidBrush myBrush = new SolidBrush(Color.White);

                    PointF playerPos = this.world.circles[playerCircle.ID].Position;

                    //PointF playerPos = playerCircle.Position;
                    float playerRadius = (float)Math.Sqrt(playerCircle._Mass / Math.PI);

                    float regionWidth = playerRadius * screenToRadiusRatio;
                    float regionLeft = playerPos.X - (regionWidth / 2);
                    float regionRight = playerPos.X + (regionWidth / 2);
                    float regionTop = playerPos.Y - (regionWidth / 2);
                    float regionBottom = playerPos.Y + (regionWidth / 2);
                    float screenWidth = 700;

                    //Loop through all recieved circles and draw on form
                    foreach (KeyValuePair<int, Circle> entry in allCircles)
                    {
                        Circle circle = entry.Value;

                        myBrush.Color = circle.CircleColor;

                        if ((int)circle.Position.X <= regionLeft || (int)circle.Position.X >= regionRight ||
                            (int)circle.Position.Y <= regionTop || (int)circle.Position.Y >= regionBottom)
                        {
                            continue;
                        }

                        float horozPixels = circle.Position.X - regionLeft;
                        float vertPixels = circle.Position.Y - regionTop;
                        float screenPosX = horozPixels / regionWidth * screenWidth;
                        float screenPosY = vertPixels / regionWidth * screenWidth;
                        float radius = (float)Math.Sqrt(circle._Mass / Math.PI) * 2;

                        e.Graphics.FillEllipse(myBrush, new Rectangle((int)screenPosX, (int)screenPosY, (int)radius, (int)radius));

                    }
                }
            }
            catch (Exception)
            {

            }
            Invalidate();
        }

        /// <summary>
        /// This method sends the position of the mouse to the server which is translated into circle movement
        /// </summary>
        private void Send_Position_To_Server()
        {
            PointF client = PointToClient(MousePosition);
            PointF mousePos = new PointF(client.X, client.Y);
            PointF playerPos = new PointF(350, 350);
            PointF direction = new PointF(mousePos.X - playerPos.X, mousePos.Y - playerPos.Y);

            Networking.Send(server.socket, new Tuple<string, int, int>("move", (int)direction.X, (int)direction.Y).ToString());
        }

        /// <summary>
        /// This method continuously recieved circle data from the server
        /// </summary>
        /// <param name="obj"></param>
        private void TheRestOfTheCircle(Preserved_Socket_State obj)
        {
            try
            {
                //Deserialize circle from server
                Circle piece = JsonConvert.DeserializeObject<Circle>(obj.Message);
                if (piece._Mass.Equals(0))
                {
                    ToRemove.Add(piece);
                }
                else
                    CirclesToAdd.Add(piece);
            }
            catch (Exception)
            {
                Debug.WriteLine($"Message is not a circle.\nMessage: {obj.Message}");
            }

            CheckAndRemove(); //Remove all the circles of mass zero

            CheckAndAdd();

            if (!obj.Has_More_Data())
            {
                Networking.await_more_data(obj);
            }

        }

        /// <summary>
        /// Intermittently removes circles from the world model
        /// </summary>
        private void CheckAndRemove()
        {
            if (ToRemove.Count > 20)
            {
                lock (world)
                {
                    world.RemoveCircles(ToRemove);
                    this.Invalidate();
                }
            }
        }

        /// <summary>
        /// Intermittently adds new circles to the world model
        /// </summary>
        private void CheckAndAdd()
        {
            if (CirclesToAdd.Count > 200)
            {
                lock (world)
                {
                    world.AddAll(CirclesToAdd);
                    this.Invalidate();
                }
                CirclesToAdd.Clear();
            }
        }

     
    }
}