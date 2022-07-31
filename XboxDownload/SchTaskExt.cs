using System;
using System.Windows.Forms;
using TaskScheduler;

namespace XboxDownload
{
    class SchTaskExt
    {
        protected static ITaskDefinition task;
        protected static ITaskFolder folder;

        public class TaskTriggerArg
        {
            public string TaskName { get; set; }                    //任务名称
            public string TaskCreator { get; set; }                 //创建者
            public string Interval { get; set; }                    //运行间隔
            public string StartBoundary { get; set; }               //开始时间
            public string EndBoundary { get; set; }                 //结束时间
            public string ActionPath { get; set; }                  //触发操作
            public string ActionArg { get; set; }                   //运行参数
            public TaskTriggerType TaskTriggerType { get; set; }    //触发类型
        }

        public enum TaskTriggerType
        {
            TASK_TRIGGER_EVENT = 0,
            TASK_TRIGGER_TIME = 1,
            TASK_TRIGGER_DAILY = 2,
            TASK_TRIGGER_WEEKLY = 3,
            TASK_TRIGGER_MONTHLY = 4,
            TASK_TRIGGER_MONTHLYDOW = 5,
            TASK_TRIGGER_IDLE = 6,
            TASK_TRIGGER_REGISTRATION = 7,
            TASK_TRIGGER_BOOT = 8,
            TASK_TRIGGER_LOGON = 9,
            TASK_TRIGGER_SESSION_STATE_CHANGE = 11,
            TASK_TRIGGER_CUSTOM_TRIGGER_01 = 12
        };

        public static void DeleteTask(string taskName)
        {
            TaskSchedulerClass ts = new TaskSchedulerClass();
            ts.Connect(null, null, null, null);
            ITaskFolder folder = ts.GetFolder("\\");
            folder.DeleteTask(taskName, 0);
        }

        public static IRegisteredTaskCollection GetAllTasks()
        {
            TaskSchedulerClass ts = new TaskSchedulerClass();
            ts.Connect(null, null, null, null);
            ITaskFolder folder = ts.GetFolder("\\");
            IRegisteredTaskCollection tasks_exists = folder.GetTasks(1);
            return tasks_exists;
        }

        public static bool IsExists(string taskName)
        {
            var isExists = false;
            IRegisteredTaskCollection tasks_exists = GetAllTasks();
            for (int i = 1; i <= tasks_exists.Count; i++)
            {
                IRegisteredTask t = tasks_exists[i];
                if (t.Name.Equals(taskName))
                {
                    isExists = true;
                    break;
                }
            }
            return isExists;
        }

        protected static void TaskInit(string taskName)
        {

            task = null;
            folder = null;

            TaskSchedulerClass scheduler = new TaskSchedulerClass();
            //pc-name/ip,username,domain,password
            scheduler.Connect(null, null, null, null);
            folder = scheduler.GetFolder("\\");


            task = scheduler.NewTask(0);
            task.RegistrationInfo.Author = "McodsAdmin";
            task.RegistrationInfo.Description = "开机启动监听 + 最小化到系统托盘";
            task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST; //使用最高权限运行

            task.Settings.ExecutionTimeLimit = "PT0S"; //运行任务时间超时停止任务吗? PTOS 不开启超时
            task.Settings.DisallowStartIfOnBatteries = false;//只有在交流电源下才执行
            task.Settings.RunOnlyIfIdle = false;//仅当计算机空闲下才执行
        }

        protected static void TaskTriggerSet(TaskTriggerArg arg)
        {
            var trigger = task.Triggers.Create((_TASK_TRIGGER_TYPE2)arg.TaskTriggerType);

            trigger.Repetition.Interval = arg.Interval;
            trigger.Enabled = true;
            trigger.StartBoundary = arg.StartBoundary;
            trigger.EndBoundary = arg.EndBoundary;

            switch (arg.TaskTriggerType)
            {
                case TaskTriggerType.TASK_TRIGGER_LOGON:
                case TaskTriggerType.TASK_TRIGGER_TIME:
                    ITrigger it = trigger;
                    break;
                case TaskTriggerType.TASK_TRIGGER_DAILY:
                    IDailyTrigger idt = (IDailyTrigger)trigger;
                    idt.DaysInterval = 1;
                    break;
            }

            //action
            IExecAction action = (IExecAction)task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
            action.Path = arg.ActionPath;
            action.Arguments = arg.ActionArg;

        }

        protected static void TaskReg(string taskName)
        {
            IRegisteredTask regTask = folder.RegisterTaskDefinition(taskName, task,
                                                    (int)_TASK_CREATION.TASK_CREATE, null, //user
                                                    null, // password
                                                    _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN,
                                                    "");
        }

        public static void CreateTaskScheduler(TaskTriggerArg arg)
        {
            try
            {
                if (IsExists(arg.TaskName))
                {
                    return;
                }
                else
                {
                    TaskInit(arg.TaskName);
                }
                TaskTriggerSet(arg);
                TaskReg(arg.TaskName);

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static void CreateRestartTask(string taskName)
        {
            int hour = 0;
            string t = hour < 10 ? "0" + hour : hour.ToString();

            SchTaskExt.TaskTriggerArg triggerArg = new SchTaskExt.TaskTriggerArg
            {
                TaskCreator = "devil",
                TaskName = taskName,
                ActionPath = Application.ExecutablePath,
                ActionArg = "Startup",
                Interval = "",
                StartBoundary = string.Format("2022-01-01T{0}:00:00", t),
                //EndBoundary = string.Format("2050-01-01T{0}:00:00", t),
                TaskTriggerType = SchTaskExt.TaskTriggerType.TASK_TRIGGER_LOGON
            };
            SchTaskExt.CreateTaskScheduler(triggerArg);
        }
    }
}
