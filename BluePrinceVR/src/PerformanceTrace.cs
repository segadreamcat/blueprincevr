using System;
using System.Diagnostics;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly double[] _frameIntervals=new double[900];
    private int _perfCount;
    private long _perfPreviousStart;
    private double _perfWaitMs,_perfBodyMs;
    private static double Milliseconds(long ticks)=>ticks*1000.0/Stopwatch.Frequency;
    private void RecordPerformance(long start,long end,double waitMs)
    {
        if(_perfPreviousStart!=0) {
            double interval=Milliseconds(start-_perfPreviousStart);
            if(interval>0 && interval<10000) _frameIntervals[_perfCount++]=interval;
        }
        _perfPreviousStart=start;
        _perfWaitMs+=waitMs;
        _perfBodyMs+=Math.Max(0,Milliseconds(end-start)-waitMs);
        if(_perfCount<_frameIntervals.Length) return;
        Array.Sort(_frameIntervals);
        double sum=0;int over=0;
        foreach(double ms in _frameIntervals) {sum+=ms;if(ms>11.1112) over++;}
        Log($"[perf] {_perfCount} frame intervals: avg={sum/_perfCount:F2}ms p50={_frameIntervals[449]:F2} p95={_frameIntervals[854]:F2} p99={_frameIntervals[890]:F2}; >11.11ms={100.0*over/_perfCount:F1}%; poseWait={_perfWaitMs/_perfCount:F2}ms VR-bodyCPU={_perfBodyMs/_perfCount:F2}ms. CPU wall timings, NOT GPU timings or proof of native refresh.");
        _perfCount=0;_perfWaitMs=_perfBodyMs=0;
    }
    private double _lastPoseWaitMs;
    private readonly double[] _stageTotals=new double[6];
    private long _stageStamp;
    private int _stageFrames;
    private void BeginPerformanceStages(){_stageStamp=Stopwatch.GetTimestamp();}
    private void PerformanceStage(int stage)
    {
        long now=Stopwatch.GetTimestamp();
        _stageTotals[stage]+=Milliseconds(now-_stageStamp);_stageStamp=now;
        if(stage!=5 || ++_stageFrames<900)return;
        Log($"[perf stages] CPU wall averages: setup={_stageTotals[0]/900:F2}ms UI={_stageTotals[1]/900:F2}ms anchors/HUD={_stageTotals[2]/900:F2}ms hands/journal={_stageTotals[3]/900:F2}ms eyes={_stageTotals[4]/900:F2}ms submit/capture={_stageTotals[5]/900:F2}ms; not GPU timings.");
        Array.Clear(_stageTotals,0,_stageTotals.Length);_stageFrames=0;
    }
}
