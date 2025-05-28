namespace Bucket.Example.AspNet.Mvc.Models;

public class TodoModel
{
    public Guid Id
    {
        get;
        set;
    } = Guid.NewGuid();

    public string Content
    {
        get;
        set;
    } = string.Empty;
}
