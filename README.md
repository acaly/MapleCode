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

## MapleCode format description

Maple code document consists of a list of nodes. Each nodes belongs to a type, and may have an optional list of string 
specifying sub-type of the node (generci arguments), several fields of different types defined by node type (arguments), 
and an optional list of children nodes.

The sub-types of a same type has no difference in arguments or whether it has a children list. It is simply an additional 
string list data. The reason to have this is to handle similar type of nodes, especially in intermediate representation of
a programming language.

Some examples of MapleCode:

```
# This is a line of comment.
node_a;                           # A node with type 'node_a', but without any other arguments.
node_b 100, -200, 0.2;            # A node with a 32-bit unsigned int, a 32-bit signed int, and a 32-bit float.
node_c 100u8, 100s16;             # A node with a 8-bit unsigned int and a 16-bit signed int.
node_d "some_str";                # A node with a string value.
node_e data u8 { 0, 1 }, data f32 { 0.1, 0.2 },
   data hex { 00 44 88 CC };      # A node with 3 typeless data segments. Sizes are 2, 8, and 4, respectively.
r1: node_f;                       # A node with label 'r1'
r2: node_g r1, r2.x, r3.y;        # A node with label 'r2', and with one node reference (to node_f) and 2 node
                                  #     references with field names (r2.x and r3.y).
r3: node_h;                       # A node with label 'r3'.
node_i "parent" { node_j; }       # A node with a string value and a child node.
```

## Binary format description

The binary format consists of 5 parts: header, string table, type table, node section, and data section.

* First byte in header specifies the *SizeMode* of the document. The 8 bit number is split into four 2-byte numbers, 
giving the size of string table index, type table index, node section offset, and data section offset, from low bits to 
high bits. 0b00 is invalid value. 0b01 is 1-byte int. 0b10 is 2-byte int. 0b11 is 4-byte int.
* The SizeMode byte is followed by the size in bytes of the 4 sections (string table, type table, node section and data 
section). The type of these size values are also given by the SizeMode. For example, if the SizeMode byte=0b10100101, then 
the size of the four sections are 1, 1, 2, 2 bytes, respectively.
* String table is a list of data section offset, where the null-terminated UTF8 string should be found. The string table 
should not contain duplicated items.
* Type table is a list of type definitions. Each type definition consists of a name (string table index), an argument type 
list (data section offset), the number of generic argument (8-bit int), and whether the node is a parent node (a 1-byte 
bool value). The first byte in argument type list is the number of elements in the list, followed by the elements of the list, 
each of which is a 1-byte value. 0=uint8, 1=uint16, 2=uint32, 3=int8, 4=int16, 5=int32, 6=float, 7=string, 8=data, 9=node 
reference, 10=node reference+field name.
* Node section consists of a continuous definition of the node hierarchy. For parent nodes, the node data is followed by the 
length of its children (as node section offset type), and the children nodes.
* Data section stores data for other sections, as defined above.

Compiling ```func "f1" { a: const<int> 1; return a; }``` gives:

```
55                        # SizeMode byte (1, 1, 1, 1)
05 0C 0B 1F               # String table size 5, type table size 12, node section size 11, data section size 31
00 05 0A 10 18            # String table (1 byte for each string, offset in data section)
01 03 00 01               # Type[0]: name=str[1], arguments=data@3, no generic argument (00), has children (01)
03 0E 01 00               # Type[1]: name=str[3], arguments=data@14, 1 generic argument (01), no children (00)
04 16 00 00               # Type[2]: name=str[4], arguments=data@22, no generic argument (00), no children (00)
00 00 08                  # Node[0] @0: type=Type[0], arguments={ str[0] }, children total length=8
01 02 01 00 00 00         # Node[1] @3: type=Type[1], generic arguments={ str[2] }, arguments={ 0x00000001 }
02 03                     # Node[2] @9: type=Type[2], arguments={ node@3 }
66 31 00                  # Data@0: string "f1"
01 07                     # Data@3: argument type { STR }
66 75 6E 63 00            # Data@5: string "func"
69 6E 74 00               # Data@10: string "int"
01 02                     # Data@14: argument type { U32 }
63 6F 6E 73 74 00         # Data@16: string "const"
01 09                     # Data@22: argument type { REF }
72 65 74 75 72 6E 00      # Data@24: string "return"
```

If the same code is compiled with external type list, it gives:

```
55                        # SizeMode byte (1, 1, 1, 1)
02 00 0B 07               # String table size 2, type table size 0, node section size 11, data section size 7
00 03                     # String table
00 00 08                  # Node[0] @0: type=Type[0], arguments={ str[0] }, children total length=8
01 01 01 00 00 00         # Node[1] @3: type=Type[1], generic arguments={ str[1] }, arguments={ 0x00000001 }
02 03                     # Node[2] @9: type=Type[2], arguments={ node@3 }
66 31 00                  # Data@0: string "f1"
69 6E 74 00               # Data@3: string "int"
```
