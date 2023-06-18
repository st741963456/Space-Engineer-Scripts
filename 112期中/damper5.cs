//Script Version
static string VERSION = "5"; 
static string SCRIPTTITLE = "Wing Damper";
//V3 : full stablization
//V5 : WASD related movement

string BlockTag = "damper";
bool HASRUNSETUP = false;
const double radian = 180 / Math.PI;

List<IMyMotorStator> SignedHinges = new List<IMyMotorStator>();
List<IMyRemoteControl> SignedRCS = new List<IMyRemoteControl>();
List<IMyCockpit> Seats = new List<IMyCockpit>();
List<IMyThrust> SignedT = new List<IMyThrust>();
IMyCockpit CONTROLLER;

public Program(){Runtime.UpdateFrequency = UpdateFrequency.Update1;}

void Main(string argument, UpdateType updateSource)
{
	Echo(SCRIPTTITLE + " version " + VERSION);
	Echo("by  NYKD SAMURAI  &  futer79\n");
	
	if(HASRUNSETUP == false)
	{
		Init();
		HASRUNSETUP = true;
	}
	
	Echo("Hinge:" + SignedHinges.Count + " RC:" + SignedRCS.Count + " Seat:" + Seats.Count + " EMT:" + SignedT.Count);
	if(SignedHinges.Count == 0 || SignedRCS.Count == 0 || Seats.Count == 0 || SignedT.Count == 0)	
	{
		Echo("Check compoments");
		return;
	}

	DamperControl();
}

void DamperControl()
{
	IMyMotorStator HingeL = SignedHinges.Find(x => x.CustomName.Contains("Hinge") == true && x.CustomName.Contains("L") == true);
	IMyMotorStator HingeR = SignedHinges.Find(x => x.CustomName.Contains("Hinge") == true && x.CustomName.Contains("R") == true);
	IMyRemoteControl RCR = SignedRCS.Find(x => x.CustomName.Contains("Right") == true);
	IMyRemoteControl RCL = SignedRCS.Find(x => x.CustomName.Contains("Left") == true);
	IMyMotorStator RotorL = SignedHinges.Find(x => x.CustomName.Contains("Rotor") == true && x.CustomName.Contains("Left") == true);
	IMyMotorStator RotorR = SignedHinges.Find(x => x.CustomName.Contains("Rotor") == true && x.CustomName.Contains("Right") == true);
	IMyThrust F1 = SignedT.Find(x => x.CustomName.Contains("F1") == true);
	IMyThrust F2 = SignedT.Find(x => x.CustomName.Contains("F2") == true);
	IMyThrust B1 = SignedT.Find(x => x.CustomName.Contains("B1") == true);
	IMyThrust B2 = SignedT.Find(x => x.CustomName.Contains("B2") == true);
	IMyThrust R1 = SignedT.Find(x => x.CustomName.Contains("R1") == true);
	IMyThrust R2 = SignedT.Find(x => x.CustomName.Contains("R2") == true);
	IMyThrust L1 = SignedT.Find(x => x.CustomName.Contains("L1") == true);
	IMyThrust L2 = SignedT.Find(x => x.CustomName.Contains("L2") == true);
	
	if (Seats.Count > 0) 
		{ 
			CONTROLLER = Seats[0] as IMyCockpit;			
			Echo("Plan A");
			
			//rotor angle
			float LAngle = RotorL.Angle;
			float RAngle = RotorR.Angle;

			//used remotecontrol ,used hinge ,rotor angle
			float angle_overL = AlignA(RCL , LAngle);
			float angle_overR = AlignA(RCR , RAngle);
			Echo("overL         " + angle_overL.ToString());	
			Echo("overR         " + angle_overR.ToString());	
			
			//get seat WASD vector
			Vector3 moveVector = CONTROLLER.MoveIndicator;
			int move_direction = VTI(moveVector);
			
			
			
			switch (move_direction)
			{
				case 0 ://none
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 0;
					break;
				case 1 ://Fr
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					F1.ThrustOverridePercentage = 0.001f;
					F2.ThrustOverridePercentage = 0.001f;
					//angle_overL += 8;
					//angle_overR += 8;
					break;
				case 2 ://Fr Ri
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					F2.ThrustOverridePercentage = 0.0006f;
					R1.ThrustOverridePercentage = 0.0006f;
					//angle_overL += 8;
					break;
				case 3 ://Ri
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					R1.ThrustOverridePercentage = 0.001f;
					R2.ThrustOverridePercentage = 0.001f;
					//angle_overL += 8;
					//angle_overR -= 8;
					break;
				case 4 ://Bk Ri
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					B1.ThrustOverridePercentage = 0.0006f;
					R2.ThrustOverridePercentage = 0.0006f;
					//angle_overR -= 8;
					break;
				case 5 ://Bk
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					B1.ThrustOverridePercentage = 0.001f;
					B2.ThrustOverridePercentage = 0.001f;
					//angle_overL -= 8;
					//angle_overR -= 8;
					break;
				case 6 ://Bk Le
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					B2.ThrustOverridePercentage = 0.0006f;
					L1.ThrustOverridePercentage = 0.0006f;
					//angle_overL -= 8;
					break;
				case 7 ://Le
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					L1.ThrustOverridePercentage = 0.001f;
					L2.ThrustOverridePercentage = 0.001f;
					//angle_overL -= 8;
					//angle_overR += 8;
					break;
				case 8 ://Fr Le
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 1;
					F1.ThrustOverridePercentage = 0.0006f;
					L2.ThrustOverridePercentage = 0.0006f;
					//angle_overR += 8;
					break;
				default:	
					foreach (IMyThrust EMT in SignedT) EMT.ThrustOverride = 0; 
					break;
			}
		
			//speed multiplier
			if (Math.Abs(angle_overL) > 0.01)
				HingeL.TargetVelocityRPM = angle_overL * 1;
			else
				HingeL.TargetVelocityRPM = 0;
			
			if (Math.Abs(angle_overR) > 0.01) 	
				HingeR.TargetVelocityRPM = angle_overR * 1;
			else	
				HingeR.TargetVelocityRPM = 0;
			
			Echo("rpmL           " + HingeL.TargetVelocityRPM.ToString());
			Echo("rpmR           " + HingeR.TargetVelocityRPM.ToString() + "\n");
		}
		else return;
	

  }


float AlignA(IMyRemoteControl RC3, float angleRP)
{
	//get RC facing orientation
	Matrix A_orientation;
	RC3.Orientation.GetMatrix(out A_orientation);

	//rotor angle to vector
	Vector3D myVector;
	myVector = new Vector3( (float)Math.Sin(angleRP), 0 , -(float)Math.Cos(angleRP) );	
	
	//get global gravity orientation
	Vector3D planet_gravity3 = RC3.GetNaturalGravity();
	
	//get gravity vector on RC3
	var _localGravity3 = Vector3D.Transform(planet_gravity3, MatrixD.Transpose(RC3.WorldMatrix.GetOrientation()));
	
	//get angle
	var _angle4 = Vector3D.Angle(_localGravity3, myVector);

	//angle - 90
	float angle_over = (float)(_angle4 * radian) - 90;	
			
	return angle_over;	
}

int VTI(Vector3 vec)
	{
		int a = 0;
		
		if (vec.Z < 0)//front
		{
			if (vec.X < 0)//left
				a = 8;
			else if (vec.X > 0)//right
				a = 2;
			else //no left or right
				a = 1;
		}
	
		else if (vec.Z > 0)//back
		{
			if (vec.X < 0)//left
				a = 6;
			else if (vec.X > 0)//right
				a = 4;
			else //no left or right
				a = 5;
		}
		
		else //no front or back
		{
			if (vec.X < 0)//left
				a = 7;
			else if (vec.X > 0)//right
				a = 3;
			else //no left or right
				a = 0;
		}
		
		return a;
	}

void Init()
{
	List<IMyMotorStator> foundHINGES = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(foundHINGES, (a => a.CustomName.Contains(BlockTag) == true));
	List<IMyRemoteControl> foundRCS = new List<IMyRemoteControl>();
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(foundRCS, (c => c.CustomName.Contains(BlockTag) == true));
	List<IMyCockpit> Controllers = new List<IMyCockpit>();
	GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Controllers, (b => b.CustomName.Contains(BlockTag) == true));
	List<IMyThrust> foundT = new List<IMyThrust>();
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(foundT, (d => d.CustomName.Contains("EMT") == true));
	
	foreach (IMyMotorStator hinge in foundHINGES)
		SignedHinges.Add(hinge);
	foreach (IMyRemoteControl RC in foundRCS)
		SignedRCS.Add(RC);
	foreach (IMyCockpit seat in Controllers)
		Seats.Add(seat);
	foreach (IMyThrust EMT in foundT)
		SignedT.Add(EMT);
}
