# microStudioCompanion
This project aims to extends possibilities of a great, yet simple browser-based game engine - microStudio. Check it out and make some games - <https://microstudio.dev>

## Features
Currently the only feature of this project is downloading all files from the specified project created in microStudio. I made it, because there is no version control in microStudio.

To use it, you have to provide your login and password for your microStudio account. The password is not stored - it vanishes after closing the program. Next requests are validated thanks to a token you get after logging in.

I have checked some possible edge cases, but I cannot guarantee it will work in all possible setups.

### Important note
**Directories** with your project files are **removed** from your local project directory before files are downloaded​. It is done to make sure you will not have offline copy of files that were deleted in microStudio.

## Plans
My target is to add ability to update and delete files on microStudio from your machine.

## Executable
You can get Windows executable from itch.io - <https://fenix.itch.io/microstudio-companion>
You can also donate me there. :)
