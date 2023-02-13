/// <summary> 
/// Author:    Hyrum Schenk
/// Partner:   none
/// Date:      April 18, 2020 
/// Course:    CS 3500, University of Utah, School of Computing 
/// Copyright: CS 3500, Hyrum Schenk - This work may not be copied for use in Academic Coursework. 
/// 
/// I, Hyrum Schenk, certify that I wrote this code from scratch and did not copy it in part or whole from  
/// another source.  All references used in the completion of the assignment are cited in my README file. 
/// 
/// File Contents 
/// 
///    This File contains code that will communicate with an SQL database. Login information is kept in a secret file.
///    The user will interact with a web browser (Microsoft Edge is the only one that seems to work so far). The user can
///    type different pre-specified commands into the URL bar. The user can add data to the database, but cannot remove data
///    from it. 
/// </summary>

using NetworkingNS;
using System;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace WebServerExample
{
    /// <summary>
    /// Initial Author: H. James de St. Germain
    /// Initial Date:   Spring 2020
    /// Main Author:    Hyrum Schenk
    /// Update Date:    4/19/2020
    /// 
    /// Code for a simple Web Server
    /// </summary>
    public class WebServer
    {
        /// <summary>
        /// keep track of how many requests have come in.  Just used
        /// for display purposes.
        /// </summary>
        static private int counter = 0;
        public static readonly string connectionString;

        /// <summary>
        /// Connect to database containing Agario player information.
        /// Login information kept in secret file.
        /// </summary>
        static WebServer()
        {
            var builder = new ConfigurationBuilder();

            builder.AddUserSecrets<WebServer>();
            IConfigurationRoot Configuration = builder.Build();
            var SelectedSecrets = Configuration.GetSection("WebServerSecrets");

            //Data is kept in a secret file
            connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = SelectedSecrets["dataSource"],
                InitialCatalog = SelectedSecrets["initialCatalog"],
                UserID = SelectedSecrets["userID"],
                Password = SelectedSecrets["password"]
            }.ConnectionString;
        }

        /// <summary>
        /// Start the program and await for connections (e.g., from the browser).
        /// Press "Enter" to end, though in a real web server, you wouldn't end.... ;^)
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Web Server Active. Awaiting Clients! Press Enter to Quit.");
            
            Networking.Server_Create_Connection_Listener(OnClientConnect);

            //Closes server
            Console.ReadLine();
        }

        /// <summary>
        /// Basic connect handler - i.e., a browser has connected!
        /// </summary>
        /// <param name="state"> Networking state object created by the Networking Code. Contains the socket.</param>
        private static void OnClientConnect(Preserved_Socket_State state)
        {
            state.on_data_received_handler = RequestFromBrowserHandler;
            Networking.await_more_data(state);
        }

        /// <summary>
        /// Create the HTTP response header, containing items such as
        /// the "HTTP/1.1 200 OK" line.
        ///        
        /// See: https://www.tutorialspoint.com/http/http_responses.htm
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string BuildHTTPResponseHeader(string message)
        {
            // modify this to return am HTTP Response header, don't forget the new line!
            return "Agario Player Database\n";
        }

        /// <summary>
        ///   Create a web page!  The body of the returned message is the web page
        ///   "code" itself. Usually this would start with the HTML tag.  Take a look at:
        ///   https://www.sitepoint.com/a-basic-html5-template/
        /// </summary>
        /// <returns> A string the represents a web page.</returns>
        private static string BuiltHTTPBody()
        {
            //Message displayed when no path is given
            return $@"
                <h1>Welcome to the Agario Player Database</h1>
                <br/>Here, you can look up data based on information from the Agario database<br/>
                <br/>This is a list of the available constraints you can use to narrow your search:<br/>
                <br/>   - / - Brings you back to this page. Alternatively, you can click the 'Home' link
                <br/>   - /highscores - Displays the high scores of all players accross their games
                <br/>   - /timealive  - Displays a table indicating each player's gameID and how long they lived
                <br/>   - /scores/'name' - show the score of each game played by a specific player
                <br/>   - /scores/'name'/'highmass'/'highrank'/'starttime'/'endtime' - Input data to be added to the database <br/>
                <br/><a href='http://localhost:11000/highscores'>View all player highscores</a><br/>
                <a href='http://localhost:11000/timealive'>See table with length of each game for all players</a>";
        }

        /// <summary>
        /// Create a response message string to send back to the connecting
        /// program (i.e., the web browser).  The string is of the form:
        /// 
        ///   HTTP Header
        ///   [new line]
        ///   HTTP Body
        ///   
        ///  The body is an HTML string.
        /// </summary>
        /// <returns></returns>
        private static string BuildHTTPResponse()
        {
            string message = BuiltHTTPBody();
            string header = BuildHTTPResponseHeader(message);

            return header + message;
        }

        /// <summary>
        ///   When a request comes in (from a browser) this method will
        ///   be called by the Networking code.  When a full message has been
        ///   read (as defined by an empty line in the overall message) send
        ///   a response based on the request.
        /// </summary>
        /// <param name="socketState"> provided by the Networking code, contains socket and message</param>
        private static void RequestFromBrowserHandler(Preserved_Socket_State socketState)
        {
            Console.WriteLine($"{++counter,4}: {socketState.Message}");
            try
            {
                // by definition if there is a new line, then the request is done
                if (socketState.Message == "\r")
                {
                    //Networking.Send(socketState.socket, BuildHTTPResponse());

                    // the message response told the browser to disconnect, but
                    // if they didn't we will do it.
                    if (socketState.socket.Connected)
                    {
                        socketState.socket.Shutdown(SocketShutdown.Both);
                        socketState.socket.Close();
                    }
                }
                //Calls method to parse user path
                else if (socketState.Message.Substring(0, 3) == "GET")
                    ParseMessage(socketState);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Something went wrong... this is a bad error message. {exception}");
            }
        }

        /// <summary>
        /// Get path and determine what the request is. Call the corrosponding method to deal
        /// with the appropriate request
        /// </summary>
        /// <param name="socketState"></param>
        private static void ParseMessage(Preserved_Socket_State socketState)
        {
            try
            {
                string urlPath = socketState.Message;
                urlPath = urlPath.Remove(0, 4);

                //No path
                if (urlPath == "/")
                {
                    BuildHTTPResponse();
                    return;
                }
                else
                {
                    urlPath = urlPath.Remove(0, 1);
                    string command = urlPath.Substring(0, urlPath.IndexOf("/"));
                    if (command.Contains(" HTTP"))
                        command = command.Remove(command.Length - 5);

                    //Get  highscores of all registered players
                    if (command == "highscores")
                        GetHighScores(socketState);

                    //Get scores of specific player or input data
                    else if (command == "scores")
                    {
                        urlPath = urlPath.Remove(0, 7);
                        string playerName = urlPath.Substring(0, urlPath.IndexOf("/"));

                        //Run if command is in format name/scores
                        if (playerName.Contains(" HTTP"))
                        {
                            playerName = playerName.Remove(playerName.Length - 5);
                            GetPlayerScores(socketState, playerName);
                        }

                        //Run if command is in format scores/name/highmass/highrank/starttime/endtime
                        else
                        {
                            urlPath = urlPath.Remove(0, playerName.Length + 1);
                            string highmass = urlPath.Substring(0, urlPath.IndexOf("/"));
                            urlPath = urlPath.Remove(0, highmass.Length + 1);
                            string highRank = urlPath.Substring(0, urlPath.IndexOf("/"));
                            urlPath = urlPath.Remove(0, highRank.Length + 1);
                            string startTime = urlPath.Substring(0, urlPath.IndexOf("/"));
                            urlPath = urlPath.Remove(0, startTime.Length + 1);
                            string endTime = urlPath.Substring(0, urlPath.IndexOf("/"));
                            if (endTime.Contains(" HTTP"))
                                endTime = endTime.Remove(endTime.Length - 5);

                            AddDataToDatabase(playerName, highmass, highRank, startTime, endTime, socketState);
                        }
                    }

                    //Call custom table method
                    else if (command == "timealive")
                        GetTableInfo(socketState);

                    else
                        Networking.Send(socketState.socket, BuildHTTPResponse());
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("There was an error parsing the URL");
            }
        }

        /// <summary>
        /// Call this method if command is in form scores/NAME/Highmass/Highrank/Starttime/Endtime
        /// </summary>
        /// <param name="playerName"></param>
        /// <param name="highmass"></param>
        /// <param name="highRank"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="socketState"></param>
        private static void AddDataToDatabase(string playerName, string highmass, string highRank, string startTime, string endTime, Preserved_Socket_State socketState)
        {
            try
            {
                int gameID = 0;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string messageForWebPage = "Agario Player Database\n";
                    messageForWebPage += "<h1>Data added to database</h1><br/>";               

                    //Insert values into GamneID table
                    using (SqlCommand command = new SqlCommand("INSERT INTO Agario_GameID VALUES ('" + playerName + "', " + highRank + ", " + startTime + ", " + endTime + ")", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("{0} {1}",
                                    reader.GetInt32(0), reader.GetString(1));
                            }
                        }
                    }

                    //Get GameID
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Agario_GameID WHERE PlayerName = '" + playerName + "'", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (gameID < reader.GetInt32(1))
                                    gameID = reader.GetInt32(1);
                            }
                        }
                    }

                    messageForWebPage = DataAddedMessage(messageForWebPage, playerName, highmass, highRank, startTime, endTime);
                    Networking.Send(socketState.socket, messageForWebPage);                 
                }

                //Call method to add data to other tables
                AddDataToOtherTables(playerName, highmass, highRank, startTime, endTime, socketState, gameID);
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
            }
        }

        /// <summary>
        /// When a user inputs data, the corrosponding data will be added to the appropriate table
        /// </summary>
        /// <param name="playerName"></param>
        /// <param name="highmass"></param>
        /// <param name="highRank"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="socketState"></param>
        /// <param name="gameID"></param>
        private static void AddDataToOtherTables(string playerName, string highmass, string highRank, string startTime, string endTime, Preserved_Socket_State socketState, int gameID)
        {
            try
            {
                bool playerExists = false;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    bool updateTable = false;

                    //Check if a player already exists in MaxSize table
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Agario_MaxSize WHERE PlayerName = '" + playerName + "'", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                playerExists = true;
                            }
                        }
                    }

                    //If player doesn't exist in MaxSize, add them to the table
                    if (playerExists == false)
                    {
                        using (SqlCommand command = new SqlCommand("INSERT Agario_MaxSize Values ('" + playerName + "', " + highmass + ")", connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {

                            }
                        }
                    }
                    //Else, check if the stores mass is smaller than the user inputted mass
                    else
                    {
                        int storedMass = 0;
                        using (SqlCommand command = new SqlCommand("SELECT * FROM Agario_MaxSize WHERE PlayerName = '" + playerName + "'", connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    storedMass = reader.GetInt32(1);
                                    if (Int32.TryParse(highmass, out int enteredMass))
                                    {
                                        if (storedMass < enteredMass)
                                            updateTable = true;
                                    }
                                }
                            }
                        }
                    }
                    //If inputted mass is greater than the stores mass, update the table
                    if (updateTable == true)
                    {
                        using (SqlCommand command = new SqlCommand("UPDATE Agario_MaxSize SET MaxSize = " + highmass + " WHERE PlayerName = '" + playerName +"'", connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {

                                }
                            }
                        }
                    }

                    //Calcualte the time alive and add the data to the TimeAlive table
                    Int32.TryParse(startTime, out int start);
                    Int32.TryParse(endTime, out int end);

                    string timeAlive = (end - start).ToString();
                    
                    using (SqlCommand command = new SqlCommand("INSERT INTO Agario_TimeAlive VALUES ('" + playerName + "', " + highmass + ", " + gameID + ", " + timeAlive + ")", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                        }
                    }
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
            }
        }

        /// <summary>
        /// Creates web page based on data given by user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="playerName"></param>
        /// <param name="highmass"></param>
        /// <param name="highRank"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        private static string DataAddedMessage(string message, string playerName, string highmass, string highRank, string startTime, string endTime)
        {
            message +=
                "Player name: " + playerName + "<br/>" +
                "High mass: " + highmass + "<br/>" +
                "High rank: " + highRank + "<br/>" +
                "Start time: " + startTime + "<br/>" +
                "End time: " + endTime + "<br/>" +
                "<a href='http://localhost:11000/'>Return to homepage</a>";
            return message;
        }

        /// <summary>
        /// Retrieve data from database, display player names followed by their respective
        /// highest scores across all played games
        /// </summary>
        /// <param name="socketState"></param>
        private static void GetHighScores(Preserved_Socket_State socketState)
        {
            try
            {
                //create instance of database connection
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the SqlConnection.
                    connection.Open();
                    string messageForWebPage = "Agario Player Database\n";
                    messageForWebPage += "<h1>Player Highscores</h1><br/>";
                    // This code uses an SqlCommand based on the SqlConnection.
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Agario_MaxSize", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            
                            while (reader.Read())
                            {
                                Console.WriteLine("{0} {1}",
                                    reader.GetString(0), reader.GetInt32(1));
                                messageForWebPage = messageForWebPage + reader.GetString(0) + ": " + reader.GetInt32(1) + "<br/>";
                            }
                        }
                    }
                    messageForWebPage += "<a href='http://localhost:11000/'>Return to homepage</a>";
                    Networking.Send(socketState.socket, messageForWebPage);
                }
                Console.WriteLine($"Successful SQL connection");
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
        }

        /// <summary>
        /// Method called if scores/NAME is typed into the URL
        /// </summary>
        /// <param name="socketState"></param>
        private static void GetPlayerScores(Preserved_Socket_State socketState, string playerName)
        {
            try
            {
                //create instance of database connection
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the SqlConnection.
                    connection.Open();
                    int TEST = 0;
                    string messageForWebPage = "Agario Player Database\n";
                    messageForWebPage += "<h1>" + playerName + "'s highscores</h1><br/>";
                    // This code uses an SqlCommand based on the SqlConnection.
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Agario_TimeAlive where PlayerName = '" + playerName + "'", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                messageForWebPage = messageForWebPage + "GameID  " + reader.GetInt32(2) + ": " + reader.GetInt32(1) + "<br/>";
                                TEST = reader.GetInt32(2);
                            }
                        }
                    }
                    messageForWebPage += "<a href='http://localhost:11000/'>Return to homepage</a>";
                    Networking.Send(socketState.socket, messageForWebPage);
                }
                Console.WriteLine($"Successful SQL connection");
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
        }

        /// <summary>
        /// When the user types the command /table, display a table showing the game ID, the player and how long
        /// they were alive for the duration of that game
        /// </summary>
        /// <param name="socketState"></param>
        private static void GetTableInfo(Preserved_Socket_State socketState)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand myCommand = new SqlCommand("SELECT * FROM Agario_TimeAlive", connection);
                    connection.Open();

                    SqlDataReader read = myCommand.ExecuteReader();

                    string webPageHTML = "Agario Player Database\n";
                    webPageHTML += "<h1>Table: Time Alive</h1><br/>";

                    webPageHTML += "<table style='width:500px'>"
                                  + "<tr><th>Game ID</th><th>Player</th><th>Time Alive (in UNIX)</th></tr>";

                    string id = "";
                    string name = "";
                    string timeAlive = "";

                    if (read.HasRows)
                    {
                        while (read.Read())
                        {
                            id = read["GameID"].ToString();

                            name = read["PlayerName"].ToString();

                            timeAlive = read["TimeAlive"].ToString();

                            webPageHTML += "<tr><td>" + id + "</td>"
                                   + "<td>" + name + "</td>"
                                   + "<td>" + timeAlive + "</td></tr>";
                        }
                    }
                    else
                    {
                        Console.WriteLine("nothing");
                    }
                    read.Close();

                    string messageForWebPage = webPageHTML + "</table>" + "<a href='http://localhost:11000/'>Return to homepage</a>"; ;

                    Networking.Send(socketState.socket, messageForWebPage);
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
        }
    }
}


