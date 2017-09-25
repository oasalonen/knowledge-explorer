# knowledge-explorer
Code for testing Azure [Knowledge Exploration Service](https://docs.microsoft.com/en-us/azure/cognitive-services/kes/overview).

## Dependencies
* Python 3.x
* KES SDK
  * Download from [Microsoft](https://www.microsoft.com/en-us/download/details.aspx?id=51488)
  * Make sure ```kes.exe``` is in your ```PATH```
* Visual Studio

## Structure
The code consists of three modules:
1. Data preprocessing in ```backend/```
2. Script to start the KES server in ```backend/```
3. A UWP client in the ```client/``` directory that can be used to test the locally running server.

## Preprocessing the data
First download the absence information from Planmill and export it to a ```;``` delimited CSV. Then, in the ```backend``` folder, run
```
python absences.py PATH_TO_ABSENCE_CSV_FILE
```
This will generate the data JSON and synonym files required to generate a KES index.

## Starting the server
Simply run the ```run-all.ps1``` script in the ```backend/``` directory. This will regenerate the KES grammar and index files, and then start the KES server on port 8000.

## Running the client
Open the client in Visual Studio and run it once the server is up and running.
