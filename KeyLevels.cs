#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class KeyLevels : Indicator
	{
//		private int		ninja_Start_Time;
//		private int		ninja_End_Time;
//		private int		ninja_IB_End_Time;
		private int		latestRTHbar;
		private int		lastBar;
		private bool 	sunday = false;
		private int 	rthStartBarNum;
		private string 	yDate;
		private double	yHigh;
		private double	yLow;
		private int 	rthEndBarNum; 
		private int 	gxBars;
		private double 	gxHigh = 0.0;
	    private double  gxLow = 0.0;
		private double  todayOpen = 0.0;
		private double  Gap_D = 0.0;
		private double  Close_D = 0.0;
		private string 	message = "no message";
		private int 	preMarketLength = 0;
		private int 	MaxGapBoxSize = 10;
		private bool 	showKeyLevels = false;
		private int 	IBLength = 0;
		private double ibigh = 0.0;
	    private double ibLow = 0.0;
		
		
		private NinjaTrader.Gui.Tools.SimpleFont myFont = new NinjaTrader.Gui.Tools.SimpleFont("Helvetica", 12) { Size = 12, Bold = false };
				
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Key Levels";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				LineColor					= Brushes.DimGray;
				VAColor						= Brushes.SkyBlue;
				PocColor					= Brushes.Red;
				GapUp						= Brushes.LimeGreen;
				GapDown						= Brushes.Red;
				RTHOpen						= DateTime.Parse("08:31", System.Globalization.CultureInfo.InvariantCulture);
				RTHClose					= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				BarsRight					= 5;
				ShowRange					= true;
				RangeHighLevel					= 4420;
				RangeVAHLevel					= 4415;
				RangePOCLevel					= 4410;
				RangeVALLevel					= 4400;
				RangeLowLevel					= 4395;
				YesterdaysPOC					= 4420;
				AddPlot(LineColor, "YHigh");
				AddPlot(LineColor, "YLow");
				AddPlot(LineColor, "OpenLine");
				AddPlot(LineColor, "GXHigh");
				AddPlot(LineColor, "GXLow");
				AddPlot(VAColor, "RangeHigh");
				AddPlot(VAColor, "RangeLow");
				AddPlot(PocColor, "RangePOC");
				AddPlot(VAColor, "RangeVAH");
				AddPlot(VAColor, "RangeVAL");
				AddPlot(PocColor, "YPOC");
				AddPlot(PocColor, "IBHigh");
				AddPlot(PocColor, "IBLow");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
		}
		
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 20 ) { return; }
			lastBar = CurrentBar - 1;
			CheckHolidayOrSunday();
			checkDaysAgo();
			if ( sunday || !showKeyLevels ) { return; }
			SessionStart();
			SessionEnd();
			RegularSession(); 
			ShowPremarketGap();
			 InitialBalance();
			Draw.TextFixed(this, "MyTextFixed", message, TextPosition.TopLeft);
		}

		private void PlotCompositeRange() {   
			if ( ShowRange ) {
				RangeHigh[0] = RangeHighLevel;
				RangeLow[0] = RangeLowLevel;
				RangePOC[0] = RangePOCLevel;
				RangeVAH[0] = RangeVAHLevel;
				RangeVAL[0] = RangeVALLevel;
				
				LineText(name: "range h", price: RangeHighLevel);
				LineText(name: "range l", price: RangeLowLevel);
				LineText(name: "range poc", price: RangePOCLevel);
				LineText(name: "range vah", price: RangeVAHLevel);
				LineText(name: "range val", price: RangeVALLevel);
			}
		}
		
		private void SessionStart() {  
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHOpen), end: ToTime(Time[0])) ) {
				rthStartBarNum = CurrentBar ;
				gxBars = rthStartBarNum - rthEndBarNum;
				todayOpen = Open[0];
				Gap_D = todayOpen - Close_D;
				//Print("Close " + Close_D + " - Open "+ todayOpen);
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString();
				
				if ( gxBars > 0 ) {
	                gxHigh = MAX(High, gxBars)[0];
	                gxLow = MIN(Low, gxBars)[0];
				}
				//Print("OPEN: " + RTHOpen + " == " +  Time[0] );
            }
		}
		
		private void SessionEnd() { 
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHClose), end: ToTime(Time[0])) ) {
				Close_D = Close[0];
				rthEndBarNum = CurrentBar;
				preMarketLength = 0;
				//Print("CLOSE: " + RTHClose + " == " +  Time[0] );
				// find RTH High + Low
				int rthLength = rthEndBarNum - rthStartBarNum;
				if ( rthLength > 0 ) {
	                yHigh = MAX(High, rthLength)[0];
	                yLow = MIN(Low, rthLength)[0];
				}
            }
		}
		
		private void RegularSession() {   
			if (IsBetween(start: ToTime(RTHOpen), end: ToTime(RTHClose))) {
				if (yHigh > 0.0 &&  yLow > 0.0) {
					YHigh[0] = yHigh; 
					YLow[0] = yLow; 					
					LineText(name: "yh", price: yHigh);
					LineText(name: "yl", price: yLow);
				} 
				
				if (gxHigh > 0.0 &&  gxLow > 0.0) {
					GXHigh[0] = gxHigh; 
					GXLow[0] = gxLow; 					
					LineText(name: "gxH", price: gxHigh);
					LineText(name: "gxL", price: gxLow);
				} 
				
				if (todayOpen > 0.0 ) {
					OpenLine[0] = todayOpen;   					
					LineText(name: "open", price: todayOpen); 
				} 
				
				PlotCompositeRange();
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString();
			}
		}
		
		private void InitialBalance() {  
			
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHOpen) +10000, end: ToTime(Time[0])) ) {
				IBLength += CurrentBar - rthStartBarNum;
				Print("IB Close " + Time[0] + " bars " + IBLength);
				ibigh = MAX(High, IBLength)[0];
	            ibLow = MIN(Low, IBLength)[0];
			}
			if (IsBetween(start: ToTime(RTHOpen) +10000, end: ToTime(RTHClose))) {
				//Draw.Text(this, "MyText"+CurrentBar, "-", 0, ibigh, Brushes.Blue);
				IBHigh[0] = ibigh;
				IBLow[0] = ibLow;
				LineText(name: "ibH", price: ibigh);
					LineText(name: "ibL", price: ibLow);
			}
		}
				
		private bool IsEqual(int start, int end) {
			if (start == end) {
				return true;
			} else { return false; }
			
		}
		private bool IsBetween(int start, int end) {
			var Now = ToTime(Time[0]) ;
			if (Now > start && Now < end) {
				return true;
			} else { return false; }
		}
		
		private void ShowPremarketGap() {   
			if (IsBetween(start: ToTime(RTHOpen) -20000, end: ToTime(RTHOpen))) { 
				double GapHigh = 0.0;
				double	GapLow = 0.0;
				
				if ( BarsInProgress == 1 ) {
					Gap_D = Close[0] - Close_D;
					preMarketLength += 1;
					GapHigh = Close[0];
					GapLow = Close_D;
				}
				BoxConstructor(BoxLength: preMarketLength, BoxTopPrice: GapHigh, BottomPrice: GapLow, BoxName: "gapBox");
				
				if ( YesterdaysPOC > 0 ) {
					YPOC[0] = YesterdaysPOC;
				}
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString() +  "  Pre Market Gap: " + Gap_D.ToString();
			}
		}
		
		private void BoxConstructor(int BoxLength, double BoxTopPrice, double BottomPrice, string BoxName) {
			if ( BoxLength < 2 || BoxTopPrice == 0.0 || BottomPrice == 0.0) { return; }
			if ( BoxLength > MaxGapBoxSize ) { BoxLength = MaxGapBoxSize; }
			double spacer = TickSize;
			//Print("BoxLength " + BoxLength + "  BoxTopPrice " + BoxTopPrice + "  BottomPrice " + BottomPrice );
			Brush	BoxColor = GapDown;
			if ( Gap_D > 0 ) {
				BoxColor = GapUp;
				spacer = -TickSize;
			}
			RemoveDrawObject(BoxName + lastBar);
			RemoveDrawObject(BoxName+ "Txt" + lastBar);
			Draw.Rectangle(this, BoxName + CurrentBar, false, BoxLength, BottomPrice, 2, BoxTopPrice, Brushes.Transparent, BoxColor, 15);
			Draw.Text(this, BoxName + "Txt" + CurrentBar, false, Gap_D.ToString(), BoxLength, BoxTopPrice + spacer, 0,  BoxColor, myFont, TextAlignment.Left, Brushes.Transparent, BoxColor, 0);
		
		}

		private void LineText(string name, double price) { 
			Draw.Text(this, name, false, name, -BarsRight, price, 0,  LineColor, myFont, TextAlignment.Left, Brushes.Transparent, LineColor, 0);
		}
		
		private void checkDaysAgo() {
			// only show open line starting 5 days ago  
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHOpen), end: ToTime(Time[0])) ) {
				//Print("inside check days "  + "  " + ToTime(Time[0]));
		        // Get the current DateTime.
		        DateTime now = DateTime.Now;
				DateTime startDTE = now.AddDays(-7);
		        // Get the TimeSpan of the difference.
		        TimeSpan elapsed = now.Subtract(startDTE);
		        // Get number of days ago.
		        double daysAgo = elapsed.TotalDays; 
				//Print("Int chart time " + ToDay(Time[0]) + " now int " + ToDay(startDTE)); 
				if ( ToDay(Time[0])  > ToDay(startDTE) ) {
					//Print("\n"+Time[0] + " is greater than 5 days ago");
					showKeyLevels = true;
				}
			}
		}
		
		private void CheckHolidayOrSunday() { 
			// bettwee sunday ope 1 pm CST and 6:30 AM CST -> check if its sunday
			if (IsBetween(start: ToTime(RTHClose) -20000, end: ToTime(RTHOpen)-20000)) {  
				DateTime myDate = Time[0];   
				string prettyDate = myDate.ToString("MM/d/yyyy");
				yDate = Time[0].DayOfWeek.ToString();
				if (yDate == "Sunday" ) { 
					sunday = true;
					Draw.Text(this, "sunday"+prettyDate, true, yDate, 0, MAX(High, 20)[1], 1, 
						Brushes.DarkGoldenrod, myFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 50);
				} else {
					sunday = false;	
				}
			}
			
			//MARK: - TODO - Holiday doesnt print anything
			/*
			foreach(KeyValuePair<DateTime, string> holiday in TradingHours.Holidays)
			{
                string dateOnly = String.Format("{0:MM/dd/yyyy}", holiday.Key);
                
               // Print("dateOnly " + dateOnly + "   today " + prettyDate);
				
                if (dateOnly == prettyDate)
                {
                    Print("\nToday is " + holiday.Value + "\n");
					sunday = true;
                    //if (Bars.IsFirstBarOfSession)
                    //{ 
                        
                        Draw.Text(this, "holiday"+prettyDate, true, holiday.Value, 0, MAX(High, 20)[1], 1, Brushes.DarkGoldenrod, myFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 50);
						Print(holiday.Value + "  " + Time[0].ToShortDateString() );
                    //}
                }
			}
			*/
		}
		
		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Line Color", Order=1, GroupName="Parameters")]
		public Brush LineColor
		{ get; set; }

		[Browsable(false)]
		public string LineColorSerializable
		{
			get { return Serialize.BrushToString(LineColor); }
			set { LineColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="VA Color", Order=2, GroupName="Parameters")]
		public Brush VAColor
		{ get; set; }

		[Browsable(false)]
		public string VAColorSerializable
		{
			get { return Serialize.BrushToString(VAColor); }
			set { VAColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="POC Color", Order=3, GroupName="Parameters")]
		public Brush PocColor
		{ get; set; }

		[Browsable(false)]
		public string PocColorSerializable
		{
			get { return Serialize.BrushToString(PocColor); }
			set { PocColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Gap Up Color", Order=4, GroupName="Parameters")]
		public Brush GapUp
		{ get; set; }

		[Browsable(false)]
		public string GapUpSerializable
		{
			get { return Serialize.BrushToString(GapUp); }
			set { GapUp = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Gap Down Color", Order=5, GroupName="Parameters")]
		public Brush GapDown
		{ get; set; }

		[Browsable(false)]
		public string GapDownSerializable
		{
			get { return Serialize.BrushToString(GapDown); }
			set { GapDown = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTH Open", Order=6, GroupName="Parameters")]
		public DateTime RTHOpen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTH Close", Order=7, GroupName="Parameters")]
		public DateTime RTHClose
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Bars Right", Order=8, GroupName="Parameters")]
		public int BarsRight
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Composite Levels", Order=9, GroupName="Composite Levels")]
		public bool ShowRange
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range High Level", Order=10, GroupName="Composite Levels")]
		public double RangeHighLevel
		{ get; set; }

		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range Low Level", Order=11, GroupName="Composite Levels")]
		public double RangeLowLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range POC Level", Order=12, GroupName="Composite Levels")]
		public double RangePOCLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range VAH Level", Order=13, GroupName="Composite Levels")]
		public double RangeVAHLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range VAL Level", Order=14, GroupName="Composite Levels")]
		public double RangeVALLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Yesterdays POC", Order=15, GroupName="Composite Levels")]
		public double YesterdaysPOC
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YHigh
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YLow
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> OpenLine
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GXHigh
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GXLow
		{
			get { return Values[4]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeHigh
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeLow
		{
			get { return Values[6]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangePOC
		{
			get { return Values[7]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeVAH
		{
			get { return Values[8]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeVAL
		{
			get { return Values[9]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YPOC
		{
			get { return Values[10]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBHigh
		{
			get { return Values[11]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBLow
		{
			get { return Values[12]; }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private KeyLevels[] cacheKeyLevels;
		public KeyLevels KeyLevels(Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return KeyLevels(Input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}

		public KeyLevels KeyLevels(ISeries<double> input, Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			if (cacheKeyLevels != null)
				for (int idx = 0; idx < cacheKeyLevels.Length; idx++)
					if (cacheKeyLevels[idx] != null && cacheKeyLevels[idx].LineColor == lineColor && cacheKeyLevels[idx].VAColor == vAColor && cacheKeyLevels[idx].PocColor == pocColor && cacheKeyLevels[idx].GapUp == gapUp && cacheKeyLevels[idx].GapDown == gapDown && cacheKeyLevels[idx].RTHOpen == rTHOpen && cacheKeyLevels[idx].RTHClose == rTHClose && cacheKeyLevels[idx].BarsRight == barsRight && cacheKeyLevels[idx].ShowRange == showRange && cacheKeyLevels[idx].RangeHighLevel == rangeHighLevel && cacheKeyLevels[idx].RangeLowLevel == rangeLowLevel && cacheKeyLevels[idx].RangePOCLevel == rangePOCLevel && cacheKeyLevels[idx].RangeVAHLevel == rangeVAHLevel && cacheKeyLevels[idx].RangeVALLevel == rangeVALLevel && cacheKeyLevels[idx].YesterdaysPOC == yesterdaysPOC && cacheKeyLevels[idx].EqualsInput(input))
						return cacheKeyLevels[idx];
			return CacheIndicator<KeyLevels>(new KeyLevels(){ LineColor = lineColor, VAColor = vAColor, PocColor = pocColor, GapUp = gapUp, GapDown = gapDown, RTHOpen = rTHOpen, RTHClose = rTHClose, BarsRight = barsRight, ShowRange = showRange, RangeHighLevel = rangeHighLevel, RangeLowLevel = rangeLowLevel, RangePOCLevel = rangePOCLevel, RangeVAHLevel = rangeVAHLevel, RangeVALLevel = rangeVALLevel, YesterdaysPOC = yesterdaysPOC }, input, ref cacheKeyLevels);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.KeyLevels KeyLevels(Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(Input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}

		public Indicators.KeyLevels KeyLevels(ISeries<double> input , Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.KeyLevels KeyLevels(Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(Input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}

		public Indicators.KeyLevels KeyLevels(ISeries<double> input , Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}
	}
}

#endregion
