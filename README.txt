GPUImageViewer
==============

WPF image viewer for Windows. Requires .NET 4.5.2.

* Executable supports opening image from first command line argument.
* Drag and drop works.
* Directory that contains opened file is sorted to order jpg and png files by name. Use (R\L)Shift+RIGHT\LEFT to switch between images.
* Arrow keys to pan image.
* Mouse wheel to zoom.
* Left mouse button to pan image.
* Escape to reset view transform (reset zoom and panning)
* Two initial transform modes - FIT and ORIGINALTOP. Fit fits image. ORIGINALTOP makes sure image is 1:1 to screen pixels, or fits screen width. Useful for reading documents. Switch between modes by pressing V key (there is no visual feedback for mode switch. Just press Escape to test it).
