/*
This code was generated by a tool. DO NOT MODIFY this code manually, unless you really know what you are doing.
 */
using System;
				
namespace IFC4
{
	/// <summary>
	/// http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/ifctasktime.htm
	/// </summary>
	internal  partial class TaskTime : SchedulingTime 
	{
		public TaskDurationEnum DurationType {get;set;}

		public String ScheduleDuration {get;set;}

		public String ScheduleStart {get;set;}

		public String ScheduleFinish {get;set;}

		public String EarlyStart {get;set;}

		public String EarlyFinish {get;set;}

		public String LateStart {get;set;}

		public String LateFinish {get;set;}

		public String FreeFloat {get;set;}

		public String TotalFloat {get;set;}

		public Boolean IsCritical {get;set;}

		public String StatusTime {get;set;}

		public String ActualDuration {get;set;}

		public String ActualStart {get;set;}

		public String ActualFinish {get;set;}

		public String RemainingTime {get;set;}

		public Double Completion {get;set;}

		public TaskTime(TaskDurationEnum durationType,
				String scheduleDuration,
				String scheduleStart,
				String scheduleFinish,
				String earlyStart,
				String earlyFinish,
				String lateStart,
				String lateFinish,
				String freeFloat,
				String totalFloat,
				Boolean isCritical,
				String statusTime,
				String actualDuration,
				String actualStart,
				String actualFinish,
				String remainingTime,
				Double completion,
				String name,
				DataOriginEnum dataOrigin,
				String userDefinedDataOrigin) : base(name,
				dataOrigin,
				userDefinedDataOrigin)
		{
			this.DurationType = durationType;
			this.ScheduleDuration = scheduleDuration;
			this.ScheduleStart = scheduleStart;
			this.ScheduleFinish = scheduleFinish;
			this.EarlyStart = earlyStart;
			this.EarlyFinish = earlyFinish;
			this.LateStart = lateStart;
			this.LateFinish = lateFinish;
			this.FreeFloat = freeFloat;
			this.TotalFloat = totalFloat;
			this.IsCritical = isCritical;
			this.StatusTime = statusTime;
			this.ActualDuration = actualDuration;
			this.ActualStart = actualStart;
			this.ActualFinish = actualFinish;
			this.RemainingTime = remainingTime;
			this.Completion = completion;
		}
	}
}