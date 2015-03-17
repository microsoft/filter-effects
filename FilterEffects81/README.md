Filter Effects
==============

A Lumia example demonstrating the use of the different filters of Lumia Imaging
SDK with camera photos. This example app uses the camera and displays the
viewfinder for taking a picture. The taken photo is then processed with the
predefined filters. The filter properties can be manipulated and the changes can
be seen in the preview image immediately. The processed image can be saved in
JPEG format to the device. You can also select an existing photo and apply an
effect to it. 

This example application is hosted in GitHub:
https://github.com/Microsoft/filter-effects

For more information on implementation, visit Lumia Developer's Library:
http://go.microsoft.com/fwlink/?LinkId=528372


1. Important classes
-------------------------------------------------------------------------------

* In `FilterEffects.Shared`:
    * `AbstractFilter`: The base class for the filters implemented by the
      application. This abstract class implements the preview image handling and
      defines the methods required to be implemented by the derived classes. The
      filters implemented by this example are:
        * `SixthGearFilter`: Lomo filter
        * `MarvelFilter`: Cartoon filter
        * `EightiesPopSongFilter`: Sketch filter
        * `SadHipsterFilter`: Antique and lomo filter
        * `SurroundedFilter`: HDR effect
    * `AppUtils`: A utility class for fetching and calculating resolutions and
      provides helper methods for managing the image data streams.
    * `ContinuationManager`: Only needed on Windows Phone by the file picker.
      Related implementation can also be found in `App.xaml.cs`.
    * `FileManager`: Provides utility methods for loading an image from a file
      and saving image to a file.
    * `DataContext`: A singleton class holding the references to image data
      streams.
* The code behind (C# files) for the following pages is implemented in
  `FilterEffects.Shared`, but the XAML files are different depending whether the
  app is build for Windows Phone or Windows:
    * `ViewfinderPage`: Implements the camera view finder and fetches saved
      images from the file system.
    * `PreviewPage`: Manages the filters and implements image management
      including saving the image into the camera roll.


2. Compatibility
-------------------------------------------------------------------------------

Compatible with Windows Phone 8.1 and Windows 8.1 (desktop and RT). The project
is dependent on Lumia Imaging SDK.

2.1 Required capabilities
-------------------------

* Windows Phone:
    * Microphone
    * Pictures Library
    * Removable Storage
    * Webcam
    * Videos Library
* Windows:
    * Internet (Client)
    * Microphone
    * Pictures Library
    * Webcam


3. License
-------------------------------------------------------------------------------

See the license text file delivered with this project. The license file is also
available online at
https://github.com/Microsoft/filter-effects/blob/master/Licence.txt


4. Version history
-------------------------------------------------------------------------------

* 2.0 Beta  Converted into universal app to support both Windows Phone 8.1 and
            Windows 8.1.
* 1.3 Upgraded to Nokia Imaging SDK 1.1, Windows version added, and new HDR
      effect - contributed by Joost van Schaik - added both to the Windows Phone
      and Windows versions. 
* 1.2 Updated to support the latest version of the Nokia Imaging SDK. Theme
      support added.
* 1.1 Performance optimisations added based on Yan's wiki article (see related
      documentation)
* 1.0.1 Invalid reference paths fixed and some updates to app icons
* 1.0 First release
* 0.8 First release candidate
