# MapleCode
The main objective of MapleCode is to provide an efficient way to serialize and transfer structural data in C++ or C#.

The design of MapleCode is similar to bson (binary json), but MapleCode has a few improvements that is essential for Maple Project. This mainly includes:

* Each node can store multiple objects, the number and types of which are specified by the type of the node.
* Node type list is stored in a separate part of the binary format, and can be provided externally to shrink file size.
* Built-in node reference that allows one node to directly refer to another. 

Although MapleCode is a binary format, it also provides a compiler written in C# to compile a text format into the binary format for development and testing.

With its flexibility, applications of MapleCode may include:

* Application configuration files (similar to .ini file).
* Bytecode format or intermediate representation for programming languages.
