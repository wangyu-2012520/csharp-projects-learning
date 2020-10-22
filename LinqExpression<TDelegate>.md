# How to use Linq Expression<TDelegate>

### Summary
* #### Compile() - Compiles the lambda expression described by the expression tree into executable code and produces a delegate that represents the lambda expression.
* #### Two way of represent lambda expression - (1) Lambda expression as executable code (2) Lambda expression as data in the form of an expression tree.

### Compile() Example:
```C#
// Lambda expression as data in the form of an expression tree.
System.Linq.Expressions.Expression<Func<int, bool>> expr = i => i < 5;
// Compile the expression tree into executable code.
Func<int, bool> deleg = expr.Compile();
// Invoke the method and print the output.
Console.WriteLine("deleg(4) = {0}", deleg(4));

/*  This code produces the following output:

    deleg(4) = True
*/
```
### Two way of represent lambda expression Example:
The following code example demonstrates how to represent a lambda expression both as executable code in the form of a delegate and as data in the form of an expression tree. It also demonstrates how to turn the expression tree back into executable code by using the Compile method.

```C#
// Lambda expression as executable code.
Func<int, bool> deleg = i => i < 5;
// Invoke the delegate and display the output.
Console.WriteLine("deleg(4) = {0}", deleg(4));

// Lambda expression as data in the form of an expression tree.
System.Linq.Expressions.Expression<Func<int, bool>> expr = i => i < 5;
// Compile the expression tree into executable code.
Func<int, bool> deleg2 = expr.Compile();
// Invoke the method and print the output.
Console.WriteLine("deleg2(4) = {0}", deleg2(4));

/*  This code produces the following output:

    deleg(4) = True
    deleg2(4) = True
*/
```
The expression tree is an in-memory data representation of the lambda expression. The expression tree makes the structure of the lambda expression transparent and explicit. You can interact with the data in the expression tree just as you can with any other data structure.

### Reference 
- https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression-1.compile?view=netcore-3.1
- https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression-1?view=netcore-3.1
