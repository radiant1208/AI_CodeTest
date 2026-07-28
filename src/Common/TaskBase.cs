

namespace PathSearch.Common
{
    public class ManagedTask
    {
        public string Name { get; set; }
        public Task Task { get; set; }
        
        public ManagedTask(string name, Task task)
        {
            Name = name;
            Task = task;
        }
    }

    public class TaskBase
    {
        public TaskBase(string name, int delayMilliSec)
        {
            _name = name;
            _delayMilliSec = delayMilliSec;
        }

        protected string _name;
        protected int _delayMilliSec;

        protected virtual void WorkRoutine(CancellationToken ct) { }
        protected virtual Task WorkRoutineAsync(CancellationToken ct) { return Task.CompletedTask; }
        protected virtual void DoFinalize() { }

        public virtual ManagedTask RunAsync(CancellationToken ct)
        {
            return new ManagedTask(_name,
            Task.Run(async () =>
            {
                try
                {
                    while (ct.IsCancellationRequested == false)
                    {
                        try
                        {
                            WorkRoutine(ct);
                            await WorkRoutineAsync(ct);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception Occurred: {ex.Message}");
                        }
                        finally
                        {
                            await Task.Delay(_delayMilliSec, ct);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Task Canceled");
                }
                finally
                {
                    DoFinalize();
                }
            }, ct));
        }
    }
}
