# Learning for Db Migration
### Summary
#### To Run Db Projects, one easy way is to bypass args by replace args with exisitng connection string. 

Example: referring code from ```args = new[] ```

```C# 
    public class Program
    {
        private static void Main(string[] args)
        {
            args = new[]
           {
               "Data Source=localhost,1433;Initial Catalog=xxx;PersistSecurityInfo=True;User ID=sa;Password=password",
               "appuser", "xxx", "supportuser", "xxx"
           };
    }
```
