# GAF Explode

[![Build status](https://ci.appveyor.com/api/projects/status/ouiqeobwkqy77hga/branch/master?svg=true)](https://ci.appveyor.com/project/MHeasell/gafexplode/branch/master)

GAF Explode is a program that explodes a Total Annihilation GAF file
into a directory, allowing you to easily edit the contents. Once you are
done editing, GAF Explode allows you to unexplode the directory back
into a GAF file.

![Screenshot](screenshot.png?raw=true)

# Usage

GAF Explode comes with a friendly GUI. You can start it by
double-clicking on GafExplode.Gui.exe.

GAF Explode can also be used on the command line.

To explode a GAF file to a directory:

    GafExplode.exe explode my_gaf.gaf my_gaf_dir

To unexplode a directory to a GAF file:

    GafExplode.exe explode my_gaf.gaf my_gaf_dir

# Directory Structure

In the top level of the chosen directory, GAF Explode will write a file
called `gaf.json`. This is a text file in JSON format that describes all
the sequences inside the GAF file.

GAF Explode will also create one subdirectory for each sequence which
contains the images used by that sequence in PNG format.
These images are referenced by the `gaf.json` file.

# Miscellaneous Notes

* GAF Explode can explode and unexplode GAF files like FX.GAF that are
  normally problematic for other tools such as GAF Builder.
* GAF Explode compresses GAF frames automatically if the compression
  would save space. This may prevent easy interoperability with GAF
  Builder, as GAF Builder does not handle compressed frames correctly.
  Sorry.
* Images must be in PNG format and must use only colours from the Total
  Annihilation colour palette. However they don't have to be indexed
  colour PNGs, they can be normal 24 or 32 bit files.
* The colour palette is built in and there is no way to supply a custom
  palette. If you care about changing the palette please get in touch.
* Did you know that each frame in a GAF sequence can be displayed for a
  different amount of time? GAF Explode exposes a `Duration` field on
  each frame that controls this. A duration of 1 means that the frame
  lasts for one in-game tick (1/30th of a second).
* One of the properties you'll see in gaf.json is `Unknown3`. This
  corresponds to the `Unknown3` field documented in the
  [GAF format notes][gaf-fmt] published at Visual Designs. Lots of GAF
  frames have this set to a non-zero value but no-one knows what it does,
  if anything. If you do discover what this does please get in touch and
  let me know.

[gaf-fmt]: http://visualta.tauniverse.com/Downloads/ta-gaf-fmt.txt
