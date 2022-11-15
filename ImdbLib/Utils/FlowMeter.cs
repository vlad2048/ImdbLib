namespace ImdbLib.Utils;

class FlowMeter
{
	public enum Unit
	{
		Second,
		Minute
	}

	private readonly Unit unit;
	private DateTime lastMeasureTime;
	private long curCount;

	public FlowMeter(Unit unit = Unit.Second)
	{
		this.unit = unit;
		lastMeasureTime = DateTime.Now;
	}

	public void Reset()
	{
		lastMeasureTime = DateTime.Now;
		curCount = 0;
	}

	public void Increment() => curCount++;

	public double Measure()
	{
		var deltaTime = DateTime.Now - lastMeasureTime;
		var deltaCount = curCount;
		Reset();
		double timeUnit = unit switch
		{
			Unit.Second => deltaTime.TotalSeconds,
			Unit.Minute => deltaTime.TotalMinutes,
			_ => throw new ArgumentOutOfRangeException($"Unit is invalid: {unit}")
		};
		var flow = deltaCount / timeUnit;
		return flow;
	}
}