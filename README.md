# RiffData

/RiffData/ is a library for writing files in the RIFF file format.

Also included is /RiffWave/, a sample implementation of Microsoft's 
WAV audio data format that writes 8kHz MuLAW formatted sound files.

The RIFF file format was really intended for writing a whole block of
audio at a time, but RiffWave supports progressive extension of the 
'data' block by updating the previously written sections of the file
by using a model based on Memory Mapped file semantics that write out
dirty blocks on Save.

The specific implementation of the Memory Mapped files included here
keeps the entire file in memory and writes the entire buffer out to 
disk in a single operation.  Extension of that class to maintain an LRU
block list and flush blocks asynchronously is left to be implemented
separately.

/ToneGenerator/ is a sample application that generates a simple Sine
wave at a specified frequency (440Hz by default) and saves the result
to WAV.

