# AlphaNumericFS

A trivial FUSE filesystem I built to get to know FUSE.

## Supported paths

Assuming you've mounted AlphaNumericFS at `/tmp/afns`:

`/tmp/afns/numeric/data` returns five lines of `0123456789`.

`/tmp/afns/alpha/data` returns five lines of `abcdefghijklmnopqrstuvwxyz`.

`/tmp/afns/unicode/U+...` returns the Unicode character (as UTF-8) for the given code point, followed by a newline.  For example, `/tmp/afns/unicode/U+263A` returns 😀.

## Installing

First, you'll need to check out and build [stephen-riley/FuseSharp](https://github.com/stephen-riley/FuseSharp).  Follow the directions there.

Then you'll need to adjust the package reference [in the `.csproj`](https://github.com/stephen-riley/AlphaNumericFS/blob/master/AlphaNumericFS/AlphaNumericFS.csproj#L24).

Next, build it.

Finally, run it with the desired mount point on the command line, eg. `AlphaNumericFS /tmp/afns`.  (Note that this program runs in the foreground.)

When you're done playing, `umount /tmp/afns`.
