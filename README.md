# Tachyon-Tools
Modding tools for Tachyon: The Fringe


This repo explores the file formats used in TTF. My hope is that these tools can be used to mod this venerable game and give it the second life that it deserves!


Currently there are two sets of tools that are available:
CBINTool - fairly complete tool for decrypting, encrypting and converting to/from text files in INI-style format.  
    In the demo of TTF many configuration files were just INI-style text files, but in the production version of the game these were serialized and encrypted.
    This tool converts between the two formats so that modding is possible.
PAKTool - not as complete, but allows for extraction of textures so far.
  PAK files are the 3d models + textures format.

Others may come as I explore... Or I might see something more shiny and drop this like a hot rock! ¯\_(ツ)_/¯



What follows is a general document explaining the various file formats used in TTF.

Here is a brief overview:

PFF - the archive file that contains all the game data. Pretty standard roll-your-own-tar.
WAV, PCX, MP3 - industry standard file formats

The rest of the files can be divided into 4 categories:

- Text files in INI format
- enCrypted BINary versions of the above - used for configuration files
- BIN files - Text associations to internal variables. Used for text lookup in various places. This format is 
	used widely in Novalogic's other games, and third party tools abound.
- other custom formats for more specific things - 3d objects, sound etc.


CBIN files are used where INI style files would have been used in the demo. They are weakly encrypted with an easily derived XOR key.
Files that use this format:
	NWS - news files
	WNG - wingman definitions
	SEN - Scripted scenes.
	SCR - Actor Script files
	OCF - Object Connection files
	MPC - Multiplayer configuration
	MNU - Menu color configuration
	JOB - Job(contract) configuration files
	ITM - Items (specifically for the store)
	HUD - Configuration for ship huds when in first-person perspective.
	DES - 3d object description files. Not the 3d model, but the mapping from the model to entities in space.
	DEF - generic configuration used by a couple of files.
	CFG - various configuration from initial game conditions to default controls.
	BOX - Some kind of UI mapping
	BDF - Background Definition Files. Basically fancy skyboxes for levels.
	BAS - layouts for the starbase interfaces. Kinda like an oldschool image map on the web.
	ANM - animations used mostly for gates. There is very little animation used in the game. 

BIN files (not to be confused with CBIN) are sort of similar to CBIN files. The only real difference I think there is is that the CBIN files seem to be able to have multiple values per key, whereas BIN files seem to only have one value per key. But who knows. Maybe I am chasing ghosts and they are identical.
BIN files are used by novalogic's TextTool. This allows developers to associate text strings with a two-level structure, called by many 3rd party tools as Groups and Variables.
They are used as lookup tables for dialog, ship names etc. I imagine this format exists for the purposes of translation.

Other formats:
	PAK - these contain 3d models and textures broken up by LOD. Conceptually similar to the 3di format used
		in a number of other novalogic games. They seem to be different. I don't yet know why they 				implemented a seperate format when they already had 3di.
	PAL - These are essentially just very small PCX files used only for their palette. I think they exist to 
		quickly swap palettes on ships etc for various factions.
	PWF - These seem to bundle the sounds.
	SPX - Level files.
	

I haven't exactly nailed down how the various events in any particular mission are scripted. Perhaps they are in the 
SPX files.

Note: The only exploration I have done is on the files packaged in the game. I have not tried to reverse engineer
the save files, nor have I done any sort of analysis of the running game.

