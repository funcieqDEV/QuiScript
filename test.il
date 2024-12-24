
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)        
  .ver 4:0:0:0
}

.assembly quiscript
{
  .ver 1:0:0:0
}

.module main.exe

.imagebase           0x00400000
.file alignment      0x00000200
.stackreserve        0x00100000
.subsystem           0x0003       
.corflags            0x00020003

.class public auto ansi abstract sealed beforefieldinit IO
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Print(string text) cil managed
  {
    .maxstack 8
    ldarg.0
    call void [mscorlib]System.Console::WriteLine(string)
    ret
  }
}
.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
.method public hidebysig void Hello()
{

.locals init (
)
ldstr "helo"
call void IO::Print(string)
ret
}

.method public static hidebysig void Main()
{

.entrypoint

.locals init (
[0] string b
)
ldstr "random string"
stloc.0
ldstr "b = "
ldloc.0
call string [mscorlib]System.String::Concat(string, string)
call void IO::Print(string)
ret
}

}