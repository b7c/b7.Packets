# b7 packets
A packet logger extension for G-Earth.

![image](https://user-images.githubusercontent.com/58299468/127137411-a77ddfdb-2b12-40f3-b443-4b65b09bef29.png)

## Composer
The composer is located at the bottom of the logger.\
Packets can be composed with text and sent to the client (`<<`) or server (`>>`).\
The composed packet must begin with a message name or header value, followed by the values.\
The following value types can be used:
- Bool: `true` or `false` (written as a byte with the value `0` or `1`)
- Byte: `b:123` (1 byte)
- Short: `s:123` (2 bytes)
- Int: `123` (4 bytes)
- Long: `123L` (8 bytes)
- String: `"hello"` (2 byte `length` + `length` number of bytes)
- Raw bytes: `[00 05 68 65 6c 6c 6f]` (writes raw hex bytes; the left is equivalent to `"hello"`)

For example, to send a Chat packet to the server with a string and two int values: `Chat "Hello, world" 0 -1`

## Structuralizer
Double click a message in the log list to send it to the structuralizer.\
You can then click the type buttons to visualize different values which can be helpful when working out packet structures.\
Click the "copy composed" button to copy the current packet structure and values to the clipboard which can be used in the composer.

![image](https://user-images.githubusercontent.com/58299468/127137435-48e7e9d9-1864-44d9-bdb0-abd39e2b98d1.png)
