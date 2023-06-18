# GAF Explode

[![Build status](https://ci.appveyor.com/api/projects/status/ouiqeobwkqy77hga/branch/master?svg=true)](https://ci.appveyor.com/project/MHeasell/gafexplode/branch/master)

GAF Explode is a program that explodes a Total Annihilation GAF file
into a directory, allowing you to easily edit the contents. Once you are
done editing, GAF Explode allows you to unexplode the directory back
into a GAF file.

![Screenshot](screenshot.png?raw=true)

# Usage

GAF Explode comes with a friendly GUI. You can start it by
double-clicking on `GafExplode.Gui.exe`.

GAF Explode can also be used on the command line.

To explode a GAF file to a directory:

    GafExplode.exe explode my_gaf.gaf my_gaf_dir

To unexplode a directory to a GAF file:

    GafExplode.exe unexplode my_gaf_dir my_gaf.gaf


There are also some additional command line forms:
- `unexplode-quantize` -- unexplode but with nearest-neighbour
  image quantization
- `unexplode-no-trim` -- unexplode but with image trimming disabled
- `explode-no-pad` -- explode but with image padding disabled

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
* GAF Explode allows you to toggle the compression of each frame.
  Textures for 3DO models must always be saved uncompressed. For other
  types of GAFs it is recommended to save them compressed, as it has been
  found that TA shows visual artifacts on some GAFs when they are saved
  uncompressed.
* Images must be in PNG format and must use only colours from the Total
  Annihilation colour palette. However they don't have to be indexed
  colour PNGs, they can be normal 24 or 32 bit files.
  If you don't want to do the colour conversion yourself,
  GAF Explode can do a very basic nearest-neighbur conversion --
  just tick the checkbox before unexploding.
* If you use a custom colour palette in your mod you can overwrite
  the provided PALETTE.PAL with your custom one and GAF Explode
  will use it.
* Did you know that each frame in a GAF sequence can be displayed for a
  different amount of time? GAF Explode exposes a `Duration` field on
  each frame that controls this. A duration of 1 means that the frame
  lasts for one in-game tick (1/30th of a second).
* One of the properties you'll see in gaf.json is `Unknown3`. This
  corresponds to the `Unknown3` field documented in the
  [GAF format notes][gaf-fmt] published at Visual Designs. Lots of GAF
  frames have this set to a non-zero value but no-one knows what it does,
  if anything. If you do discover what this does please get in touch.
* When exploding, you can optionally have GAF Explode insert padding
  into images so that all the images in a sequence are the same size.
  Turn this off if you want to see the exact raw images contained
  in the GAF.
* Similarly when unexploding you can optionally have GAF Explode trim
  transparent colour from the borders of your images and automatically
  tweak the frame / layer origins to match.
  This makes the final GAF file smaller.
  You should probably always leave this on unless you discover some
  reason reason why you need to turn it off.
  For example, there might be somewhere in the game that doesn't respect
  the frame origin coordinates, and you really want to make the image
  appear lower by padding it with transparent colour at the top.
  Please let me know if you do encounter such an issue.

[gaf-fmt]: http://visualta.tauniverse.com/Downloads/ta-gaf-fmt.txt
