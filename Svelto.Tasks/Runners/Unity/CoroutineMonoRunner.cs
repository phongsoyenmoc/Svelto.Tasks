#if UNITY_5 || UNITY_5_3_OR_NEWER
using Svelto.Tasks.Unity.Internal;

namespace Svelto.Tasks.Unity
{
    /// <summary>
    /// while you can istantiate a MonoRunner, you should use the standard one
    /// whenever possible. Istantiating multiple runners will defeat the
    /// initial purpose to get away from the Unity monobehaviours
    /// internal updates. MonoRunners are disposable though, so at
    /// least be sure to dispose of them once done
    /// </summary>
    public class CoroutineMonoRunner : MonoRunner
    {
        public CoroutineMonoRunner(string name, bool mustSurvive = false)
        {
            _platformProfiler = new Svelto.Common.PlatformProfiler(name);
            UnityCoroutineRunner.InitializeGameObject(name, ref _go, mustSurvive);

            RunnerBehaviour runnerBehaviour = _go.AddComponent<RunnerBehaviour>();
            var runnerBehaviourForUnityCoroutine = _go.AddComponent<RunnerBehaviour>();

            _info = new UnityCoroutineRunner.StandardRunningTaskInfo { runnerName = name };

            runnerBehaviour.StartCoroutine(new UnityCoroutineRunner.Process(
                _newTaskRoutines, _coroutines, _flushingOperation, _info,
                 UnityCoroutineRunner.StandardTasksFlushing,
                 runnerBehaviourForUnityCoroutine, StartCoroutine));
        }
        
        public override void StartCoroutine(IPausableTask task)
        {
            paused = false;

            if (ExecuteFirstTaskStep(task) == true)
                _newTaskRoutines.Enqueue(task); //careful this could run on another thread!
        }

        bool ExecuteFirstTaskStep(IPausableTask task)
        {
            if (task == null)
                return false;

            //if the runner is not ready to run new tasks, it
            //cannot run immediatly but it must be saved
            //in the newTaskRoutines to be executed once possible
            if (isStopping == true)
                return true;
            
#if TASKS_PROFILER_ENABLED
            return Svelto.Tasks.Profiler.TaskProfiler.MonitorUpdateDuration(task, _info.runnerName);
#else
            bool value;
            using (_platformProfiler.Sample(_info.runnerName.FastConcat("+", task.ToString())))
            {
                value = task.MoveNext();
            }

            return value;
#endif            
        }

        public override void Dispose()
        {
            _platformProfiler.Dispose();
            
            base.Dispose();
        }

        readonly UnityCoroutineRunner.RunningTasksInfo _info;
        readonly Svelto.Common.PlatformProfiler _platformProfiler;
    }
}
#endif