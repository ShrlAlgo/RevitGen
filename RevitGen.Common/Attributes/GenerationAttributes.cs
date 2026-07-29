using System;

namespace RevitGen.Attributes
{
    /// <summary>
    /// 标记命令可用性判断方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandAvailabilityAttribute : Attribute { }

    /// <summary>
    /// 标记需要生成 IExternalEventHandler 的 partial 类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RevitExternalEventAttribute : Attribute
    {
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// 标记 ExternalEvent 的执行入口。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ExternalEventHandlerAttribute : Attribute { }

    /// <summary>
    /// 标记需要生成 IUpdater 实现的 partial 类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RevitUpdaterAttribute : Attribute
    {
        public RevitUpdaterAttribute(string name, string guid)
        {
            Name = name;
            Guid = guid;
        }

        public string Name { get; }
        public string Guid { get; }
        public int Priority { get; set; } = 100;
    }

    /// <summary>
    /// 标记 Updater 的执行入口。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class UpdaterHandlerAttribute : Attribute { }

    /// <summary>
    /// 声明 Updater 的常用触发条件。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class UpdaterTriggerAttribute : Attribute
    {
        public UpdaterTriggerAttribute(int builtInCategory, RevitChangeType changeType)
        {
            BuiltInCategory = builtInCategory;
            ChangeType = changeType;
        }

        public int BuiltInCategory { get; }
        public RevitChangeType ChangeType { get; }
        public int BuiltInParameter { get; set; }
    }

    /// <summary>
    /// Updater 支持的变更类型。
    /// </summary>
    public enum RevitChangeType
    {
        Addition,
        Deletion,
        Geometry,
        Parameter,
        Any
    }

    /// <summary>
    /// 标记包含 Revit 事件方法的 partial 类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RevitEventContainerAttribute : Attribute { }

    /// <summary>
    /// 标记需要自动订阅的 Revit 事件方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class RevitEventAttribute : Attribute
    {
        public RevitEventAttribute(RevitEventKind kind) { Kind = kind; }
        public RevitEventKind Kind { get; }
    }

    /// <summary>
    /// 支持自动订阅的 Revit 事件。
    /// </summary>
    public enum RevitEventKind
    {
        Idling,
        ViewActivated,
        DialogBoxShowing,
        DocumentOpened,
        DocumentClosing,
        DocumentClosed,
        DocumentChanged,
        DocumentSaved,
        DocumentSynchronizedWithCentral
    }

    /// <summary>
    /// 标记需要自动注册的 DockablePane partial 类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RevitDockablePaneAttribute : Attribute
    {
        public RevitDockablePaneAttribute(string guid, string title)
        {
            Guid = guid;
            Title = title;
        }

        public string Guid { get; }
        public string Title { get; }
        public DockablePanePosition InitialPosition { get; set; } = DockablePanePosition.Left;
        public int MinimumWidth { get; set; }
        public int MinimumHeight { get; set; }
    }

    /// <summary>
    /// DockablePane 初始停靠位置。
    /// </summary>
    public enum DockablePanePosition
    {
        Left,
        Right,
        Top,
        Bottom,
        Floating,
        Tabbed
    }

    /// <summary>
    /// 标记需要生成 Extensible Storage 读写代码的 partial 类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RevitSchemaAttribute : Attribute
    {
        public RevitSchemaAttribute(string guid, string name)
        {
            Guid = guid;
            Name = name;
        }

        public string Guid { get; }
        public string Name { get; }
        public string Documentation { get; set; } = "";
        public string VendorId { get; set; } = "";
    }

    /// <summary>
    /// 标记需要写入 Extensible Storage 的属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RevitSchemaFieldAttribute : Attribute
    {
        public string Name { get; set; } = "";
        public string Documentation { get; set; } = "";
    }

    /// <summary>
    /// 声明共享参数定义和绑定信息。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class RevitSharedParameterAttribute : Attribute
    {
        public RevitSharedParameterAttribute(string guid, string name)
        {
            Guid = guid;
            Name = name;
        }

        public string Guid { get; }
        public string Name { get; }
        public int ParameterGroup { get; set; }
        public SharedParameterBindingKind BindingKind { get; set; }
        public int[] Categories { get; set; } = new int[0];
        public bool Visible { get; set; } = true;
    }

    /// <summary>
    /// 共享参数绑定方式。
    /// </summary>
    public enum SharedParameterBindingKind
    {
        Instance,
        Type
    }
}
