namespace Bucket.Example.AspNet.Mvc.Controllers;

using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Models;

using Sdk;

public class TodoController: Controller
{
    private static readonly Dictionary<Guid, TodoModel> _todos = [];

    [FeatureRestricted("list-todos")]
    public IActionResult Index() => View(_todos.Values);

    [FeatureRestricted("get-todo")]
    public IActionResult Details(Guid id) => _todos.TryGetValue(id, out var todo) ? View(todo) : NotFound();

    [FeatureRestricted("create-todo")]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [FeatureRestricted("create-todo")]
    public IActionResult Create(TodoModel todo)
    {
        if (ModelState.IsValid)
        {
            _todos.Add(todo.Id, todo);
            return RedirectToAction(nameof(Index));
        }

        return View(todo);
    }

    [FeatureRestricted("delete-todo")]
    public IActionResult Delete(Guid id) => _todos.TryGetValue(id, out var todo) ? View(todo) : NotFound();

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [FeatureRestricted("delete-todo")]
    public IActionResult DeleteConfirmed(Guid id) => _todos.Remove(id, out _) ? RedirectToAction(nameof(Index)) : NotFound();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult Unauthorized() => View();
}
