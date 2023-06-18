static string VERSION = "Stirling";
static string SCRIPTTITLE = "Bomb Sight";

string BlockTag = "BombSight";
bool HASRUNSETUP = false;
const double radian = 180 / Math.PI;
const float G = 9.81f;
bool playing = false;

List<IMyCameraBlock> SignedCams = new List<IMyCameraBlock>();
List<IMyCockpit> Seats = new List<IMyCockpit>();
List<IMyRemoteControl> SignedRCS = new List<IMyRemoteControl>();
List<IMyMotorStator> SignedRotors = new List<IMyMotorStator>();
List<IMyTextPanel> SignedTexts = new List<IMyTextPanel>();
List<IMyThrust> SignedT = new List<IMyThrust>();
List<IMySoundBlock> SignedS = new List<IMySoundBlock>();
IMyCameraBlock RF;
IMyCockpit CONTROLLER;
IMyRemoteControl RC;
IMySoundBlock SO;

public Program(){Runtime.UpdateFrequency = UpdateFrequency.Update1;}

void Main(string argument, UpdateType updateSource)
{	
	Echo(SCRIPTTITLE + " version " + VERSION);
	Echo("by  NYKD SAMURAI");
	
	int MaxDis = 9999;
	float MyAlt = 0f;
	int TargetRange = 0;
	double TargetHeight = 0;
	int old_alt = 0;
	int MaxSpeed = 0;
	Vector3D L_V;
	
	if(HASRUNSETUP == false)
	{
		Init();
		HASRUNSETUP = true;
	}
	
	if (SignedCams.Count==0 || Seats.Count==0 || SignedRCS.Count==0 || SignedRotors.Count<5 || SignedTexts.Count<4) 
	{
		Echo("check block nametag or count");
		return;	
	}
	else 
	{				
		RF = SignedCams[0] as IMyCameraBlock;
		RF.EnableRaycast = true;
		CONTROLLER = Seats[0] as IMyCockpit;
		RC = SignedRCS[0] as IMyRemoteControl;
		SO = SignedS[0] as IMySoundBlock;		
		IMyMotorStator RotorAA = SignedRotors.Find(a => a.CustomName.Contains("AA") == true);
		IMyMotorStator RotorBB = SignedRotors.Find(b => b.CustomName.Contains("BB") == true);
		IMyMotorStator RotorCC = SignedRotors.Find(c => c.CustomName.Contains("CC") == true);
		IMyMotorStator Manual_Aim = SignedRotors.Find(d => d.CustomName.Contains("Manual") == true);
		IMyMotorStator Auto_Aim = SignedRotors.Find(e => e.CustomName.Contains("Auto") == true);
		IMyTextPanel ATTPanel = SignedTexts.Find(f => f.CustomName.Contains("ATT") == true);
		IMyTextPanel InputDis = SignedTexts.Find(g => g.CustomName.Contains("Input") == true);
		IMyTextPanel LastPanel = SignedTexts.Find(h => h.CustomName.Contains("Last") == true);
		IMyTextPanel SpeedPanel = SignedTexts.Find(i => i.CustomName.Contains("Speed") == true);
		
		//Echo("RangeFinder : " + RF.CustomName);
		//Echo("Seat : " + CONTROLLER.CustomName);
		//Echo("Remote : " + RC.CustomName);		
		//Echo("AvailableScanRange " + RF.AvailableScanRange.ToString());
		//Echo("EnableRaycast " + RF.EnableRaycast.ToString());
		//Echo("RaycastDistanceLimit " + RF.RaycastDistanceLimit.ToString());
		//Echo("RaycastTimeMultiplier " + RF.RaycastTimeMultiplier.ToString() + "\n");
		
			
		//RangeFinder And Height Part
		Echo("last valid range : " + LastPanel.GetText() + "m");
		int.TryParse(InputDis.GetText(), out MaxDis);
		int.TryParse(LastPanel.GetText(), out TargetRange);
		int.TryParse(SpeedPanel.GetText(), out MaxSpeed);
		int.TryParse(this.Storage, out old_alt);
		MyAlt = GetMyAlt(CONTROLLER);
		Echo("my sea level height : " + MyAlt.ToString() + "m");		
		if(RF.AvailableScanRange >= MaxDis)
		{
			TargetRange = Range(RF, MaxDis, LastPanel);
			this.Storage = MyAlt.ToString();
		}		
		float MA_Radian = Manual_Aim.Angle * 1;
		double ranged_height = Math.Cos(MA_Radian) * TargetRange;//seat or RF height offset
		int target_alt = old_alt - (int)Math.Round(ranged_height);
		Echo("target sea level height : " + target_alt.ToString() + "m");
		TargetHeight = MyAlt - target_alt;
		Echo("target surface below me : " + TargetHeight.ToString() + "m");
		
		//Triple Axis Sight Stablizer
		float AAAngle = RotorAA.Angle;//mount on left
		float BBAngle = RotorBB.Angle;//mount on back
		double ADegree = (double)AAAngle * radian;
		double BDegree = (double)BBAngle * radian;
		//Echo("AA :" + ADegree.ToString());
		//Echo("BB :" + BDegree.ToString());
		Stablize(RC, "", RotorAA, AAAngle);
		Stablize(RC, "BB", RotorBB, BBAngle);
		Stablize2(RC, RotorCC, out L_V);
		
		//cruise control
		MaxSpeed *= -1;
		if (L_V.Z <= MaxSpeed)
			foreach (IMyThrust th in SignedT)	th.Enabled = false;
		else if (L_V.Z <= -10 && L_V.Z >= MaxSpeed)
			foreach (IMyThrust th in SignedT)	th.Enabled = true;
		
		//Aim To Predicted Point
		if(Math.Pow(ADegree,2) + Math.Pow(BDegree,2) <= 100)
		{
			BombPredict(L_V, (int)TargetHeight, MA_Radian, Auto_Aim);
			ATTPanel.WriteText("Good\nAttitude");
			ATTPanel.FontColor = Color.Green;			
		}
		else
		{
			ATTPanel.WriteText("Bad\nAttitude");
			ATTPanel.FontColor = Color.Red;
		}
		
		//aim angle fit, bombs out
		double fit = Math.Abs((Manual_Aim.Angle * radian) - (Auto_Aim.Angle * radian));
		if (fit < 3 && fit > -1)
		{
			if (!playing)	
			{
				SO.Play();
				playing = true;
			}
		}
		else
		{
			SO.Stop();
			playing = false;
		}
	}	
	
	
}//Main

void BombPredict(Vector3D LV, int S, float MA_Radian, IMyMotorStator AU)
{
	float AU_Radian = AU.Angle;
	double Y = Math.Round(LV.Y * 100) / 100;	//+Y=Up
	double Z = Math.Round(LV.Z * 100) / 100;	//+Z=Back
	//Re-Define Z
	Z *= -1;	//+Z=Front
	Echo("system forward speed : " + Z.ToString());
	
	//Calculate Falling Time
	double T = 0;
	if (Math.Abs(Y) < 5)	//no up or down
		T = Math.Sqrt(2 * S / G);	//S = 1/2*GT^2
	else if (Y >= 5)	//up
	{
		int SS = S;
		double T1 = Y / G;	//0 to H_max time
		double H = Math.Round((Y * Y)/(2 * G));	//additional height
		SS += (int)H;	//H_max
		double T2 = Math.Sqrt(2 * SS / G);	//H_max to ground time
		T = T1 + T2;
	}
	else if (Y <= -5)
	{		
		double K = (Y * Y) + (2 * G * S);
		T = (Y + Math.Sqrt(K)) / G;
	}	
	T = Math.Round(T *100) / 100;
	Echo("bomb falling time : " + T.ToString() + "s");
	
	if (Z < 0)	Z = 0;
	double trail = Z * T;
	trail = Math.Round(trail * 100) / 100;
	Echo("bomb trail range : " + trail.ToString() + "m");
	
	double AU_target = Math.Atan2(trail, S);
	double over = (AU_target - AU_Radian) * 1;
	AU.TargetVelocityRad = (float)over;
}//BombPredict

void Stablize2(IMyRemoteControl RC4, IMyMotorStator CC, out Vector3D Local_Velocity)
{
	//The system's front-side would align to current moving direction
	
	//Get Plane Speed Vector
	Vector3D World_Velocity = RC4.GetShipVelocities().LinearVelocity;
	Local_Velocity = Vector3D.TransformNormal(World_Velocity,MatrixD.Transpose(RC4.WorldMatrix));	
	
	
	double current = (double)CC.Angle * radian;
	double over = 0;
	if (Local_Velocity.Z < -10)//has forward speed
	{
		Matrix rc_orientation;
		RC4.Orientation.GetMatrix(out rc_orientation);
		Vector3D rc_left = rc_orientation.Left;	
		var local_left = Vector3D.Transform(rc_left, MatrixD.Transpose(rc_orientation));
		var angle5 = Vector3D.Angle(Local_Velocity, local_left) * radian - 90;
		//Echo(angle5.ToString());		
		over = angle5 - current;	
	}
	else	over = current * -1;//back to 0
		
	//speed multiplier
	if (Math.Abs(over) < 0.05)
		CC.TargetVelocityRPM = 0;
	else if (Math.Abs(over) < 1)
		CC.TargetVelocityRPM = (float)over * 2;
	else CC.TargetVelocityRPM = Math.Sign(over) * 2;	
	
}//Stablize2

void Stablize(IMyRemoteControl RC3, string dir, IMyMotorStator rotor, float angleRP)
{
	//The system's down-side would always face to earth
	
	//get RC facing orientation
	Matrix my_orientation;
	RC3.Orientation.GetMatrix(out my_orientation);
	
	//choose RC vector
	Vector3D my_dir;
	if (dir == "BB")
		my_dir = my_orientation.Left;
	else
		my_dir = my_orientation.Forward;
	
	//get global gravity orientation
	Vector3D planet_gravity3 = RC3.GetNaturalGravity();	
	
	//get gravity vector on RC3
	var _localDir = 	Vector3D.Transform(my_dir, MatrixD.Transpose(my_orientation));		
	var _localGravity3 = Vector3D.Transform(planet_gravity3, MatrixD.Transpose(RC3.WorldMatrix.GetOrientation()));

	//get angle
	var _angle4 = Vector3D.Angle(_localGravity3, _localDir);
	
	//angle - 90
	float angle_over = (float)(_angle4 * radian) - 90;
	//Echo("over         " + angle_over.ToString());
	
	//speed multiplier
	if (Math.Abs(angle_over) < 0.1 )	
		rotor.TargetVelocityRPM = 0;
	else if (Math.Abs(angle_over) < 1 )
		rotor.TargetVelocityRPM = angle_over * 4;
	else	
		rotor.TargetVelocityRPM = (float)Math.Sign(angle_over) * 4;
}//Stablize

int Range(IMyCameraBlock camera, int maxdis, IMyTextPanel panel)
{			
	var entity = camera.Raycast( maxdis, 0, 0 );
	if( !entity.IsEmpty() ) 
	{
		var type = entity.Type;
		if( type == MyDetectedEntityType.Planet || type == MyDetectedEntityType.LargeGrid)
		{
			double dis = Vector3D.Distance((Vector3D)entity.HitPosition,camera.GetPosition());
			dis = Math.Round(dis);
			panel.WriteText(dis.ToString());
			return (int)dis;
		}
		else return maxdis;
	}
	else return maxdis;
}

int GetMyAlt(IMyCockpit seat)
{
	double ALT;	
	seat.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out ALT);	
	ALT = Math.Round(ALT) - 1;
	return (int)ALT;
}

void Init()
{
	List<IMyCameraBlock> foundCams = new List<IMyCameraBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(foundCams, (a => a.CustomName.Contains(BlockTag) == true));
	List<IMyCockpit> Controllers = new List<IMyCockpit>();
	GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Controllers, (b => b.CustomName.Contains(BlockTag) == true));
	List<IMyRemoteControl> foundRCS = new List<IMyRemoteControl>();
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(foundRCS, (c => c.CustomName.Contains(BlockTag) == true));
	List<IMyMotorStator> foundRotors = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(foundRotors, (d => d.CustomName.Contains(BlockTag) == true));
	List<IMyTextPanel> foundTexts = new List<IMyTextPanel>();
	GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(foundTexts, (e => e.CustomName.Contains(BlockTag) == true));
	List<IMyThrust> foundT = new List<IMyThrust>();
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(foundT, (f => f.CustomName.Contains(BlockTag) == true));
	List<IMySoundBlock> foundSound = new List<IMySoundBlock>();
	GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(foundSound, (g => g.CustomName.Contains(BlockTag) == true));

	foreach (IMyCameraBlock cam in foundCams)
		SignedCams.Add(cam);
	foreach (IMyCockpit seat in Controllers)
		Seats.Add(seat);
	foreach (IMyRemoteControl RC in foundRCS)
		SignedRCS.Add(RC);
	foreach (IMyMotorStator rotor in foundRotors)
		SignedRotors.Add(rotor);
	foreach (IMyTextPanel text in foundTexts)
		SignedTexts.Add(text);
	foreach (IMyThrust th in foundT)
		SignedT.Add(th);
	foreach (IMySoundBlock so in foundSound)
		SignedS.Add(so);			
}