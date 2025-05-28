using Bucket.Sdk;

using Microsoft.AspNetCore.Mvc;

[ApiController, Route("todos"), FeatureRestricted("list-todos")]
public sealed class TodoController: Controller
{
    private static readonly Dictionary<Guid, Todo> _todos = [];

    [HttpGet] public IActionResult GetAll() => Ok(_todos.Values);

    [HttpGet("{id:guid}"), FeatureRestricted("get-todo")]
    public IActionResult GetById(Guid id) => _todos.TryGetValue(id, out var todo) ? Ok(todo) : NotFound();

    [HttpPost, FeatureRestricted("create-todo")]
    public IActionResult Create([FromBody] Todo todo)
    {
        _todos.Add(todo.Id, todo);
        return Ok(todo);
    }

    [HttpDelete("{id:guid}"), FeatureRestricted("delete-todo")]
    public IActionResult Delete(Guid id) => _todos.Remove(id, out var todo) ? Ok(todo) : NotFound();
}
