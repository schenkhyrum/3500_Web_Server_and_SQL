# assignment-9-web-server-and-sql-pan-c
assignment-9-web-server-and-sql-pan-c created by GitHub Classroom  
Author:     Hyrum Schenk
Partner:    none
Date:       4/18/2020 
Course:     CS 3500, University of Utah, School of Computing  
GitHub ID:  schenkhyrum
Repo:       https://github.com/uofu-cs3500-spring20/assignment-9-web-server-and-sql-pan-c  
Commit #:   14347518031f869b8f18e802c2e323a005648ead  
Assignment: Assignment 9 - Web Server and SQL
Copyright:  CS 3500 Hyrum Schenk - This work may not be copied for use in Academic Coursework.  

## 1. Comments to Evaluators:

The extra command that was added according the YOUR CHOICE criteria in part 8 of the assignment is called with the following line:
localhost:11000/timealive. This will display a table of the GameID, the player's name and how long that specific match lasted for
that player. The time entered is in UNIX format, recognized as an Int32 type.

## 2. Assignment Specific Topics

This code was tested soley using the Microsoft Edge browser. For unknown reasons, this server failes to connect with the Chrome browser.
It is unknown if the server will communicate with other browsers. I recommend using Microsoft Edge during testing. The agario client from
assignment 8, which is in the github repo https://github.com/uofu-cs3500-spring20/assignment-eight-agario-client-bargain_bin has been updated
and is now capable of sending data to the SQL database. However, due to the nature of the newly added DeathHandler method, the client can
only be played once and has to be closed and reopened if the user wants to play another game. Think of this as a 'hardcore mode'.

### Estimated Completion Time

Assignment # | Estimated/Actual hours | Notes
:---- | :---: | -------
Assignment 9 | 22 / 16 | About 6 hours was put into learning material, 5 hours was put into coding, roughly 2 hours went into documentation. The
remainder was spent on debugging

### Partnership

This project was done solo by Hyrum Schenk

#### Hyrum Schenk's branches

1. CreatingConnection

### Database Table Summary
- I created 4 different tables for my database. One is used for tracking the highest rank a player achieved during a single game. This also tracks the start and end time of that game. The second table tracks the maximum size a player has achieved across all games. The third table keeps track of when a player achieved rank 1 during any game. If they don't achieve rank 1 during a game, the stat is not recorded. The last table tracks the score and total time alive for every match played.

### Testing
- When coding interactions with the database, testing was done in the SSMS suite. When testing the overall functionality and interactions
with the web server, testing was done using Visual Studio with Microsoft Edge used as the input tool.

## 3. Consulted Peers:

- Jolie UK (TA)
- Logan Terry (TA)

## 4. References:

- Parsing strings to ints - https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-a-string-to-a-number
- Updating tables and databases - https://www.w3schools.com/sql/sql_update.asp
- Skeleton for table - https://stackoverflow.com/questions/53117094/c-sharp-sql-database-to-html-table
