using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class ArduinoPlayChecks : MonoBehaviour {
 public InputActionAsset asset;
 private int checks;
 void Check(bool value,string label) { if(!value) throw new Exception(label); checks++; }
 void Pump() { typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Manual}); }
 void TickManager(ardunoManager manager) { typeof(ardunoManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null); Pump(); }
 void Seed(ardunoManager.ArduinoSlot slot,int x,int y,bool fire,bool stale=false,bool button2=false,bool switchSide=false) {
  if(slot.connection==null) slot.connection=new ArduinoSerialConnection(slot.portName);
  slot.attemptedPort=slot.portName;
  typeof(ArduinoSerialConnection).GetField("latest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(slot.connection,
   new ArduinoSerialConnection.Snapshot {x=x,y=y,fire=fire,button2=button2,switchSide=switchSide,receivedAt=System.Diagnostics.Stopwatch.GetTimestamp()-(stale ? System.Diagnostics.Stopwatch.Frequency*2:0),status="Simulated serial data"});
 }
 IEnumerator Start() {
  yield return null;
  try { Run(); File.WriteAllText("arduino-play-checks-passed.txt",checks+" checks passed in Play mode."); Debug.Log("ARDUINO PLAY CHECKS PASSED: "+checks); UnityEditor.EditorApplication.Exit(0); }
  catch(Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); }
 }
 void Run() {
  InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  var keyboard=InputSystem.AddDevice<Keyboard>(); var mouse=InputSystem.AddDevice<Mouse>();
  var managerObject=new GameObject("Manager checks"); var manager=managerObject.AddComponent<ardunoManager>(); manager.releaseWhenUnfocused=false; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  TickManager(manager); Check(manager.slots[0].device==null,"Zero Arduino setup");
  var players=new PlayerInput[4];
  for(int i=0;i<4;i++) {
   var obj=new GameObject("Player "+i); obj.SetActive(false);
   var pi=obj.AddComponent<PlayerInput>(); pi.actions=asset; pi.defaultActionMap="Player";
   pi.notificationBehavior=PlayerNotifications.InvokeCSharpEvents; pi.neverAutoSwitchControlSchemes=true;
   obj.SetActive(true); players[i]=pi;
   manager.slots[i].player=pi; manager.slots[i].portName="SIM"+i;
  }
  players[0].SwitchCurrentControlScheme("Keyboard&Mouse",keyboard,mouse);
  manager.slots[0].enabled=true; Seed(manager.slots[0],1023,512,true); TickManager(manager);
  Debug.Log("Batch focused: "+Application.isFocused+" status: "+manager.slots[0].status);
  Debug.Log("Device enabled="+manager.slots[0].device.enabled+" value="+manager.slots[0].device.move.ReadValue()+" fire="+manager.slots[0].device.fire.ReadValue()+" actionEnabled="+players[0].actions.FindAction("Player/Move").enabled+" controls="+players[0].actions.FindAction("Player/Move").controls.Count+" actionValue="+players[0].actions.FindAction("Player/Move").ReadValue<Vector2>()+" fresh="+ArduinoSerialConnection.IsFresh(manager.slots[0].connection.ReadSnapshot().receivedAt,0.5f)+" releaseUnfocused="+manager.releaseWhenUnfocused); Check(players[0].currentControlScheme=="Arduino","Arduino scheme selected");
  Check(players[0].actions.FindAction("Player/Move").ReadValue<Vector2>().x>0.99f,"Single Arduino feeds Move");
  Check(players[0].actions.FindAction("Player/Attack").IsPressed(),"Single Arduino feeds Attack");
  for(int i=1;i<4;i++) { manager.slots[i].enabled=true; Seed(manager.slots[i],512,512,false); }
  TickManager(manager);
  for(int i=0;i<4;i++) {
   Debug.Log("Slot "+i+" status="+manager.slots[i].status+" assigned="+manager.slots[i].device+" actual="+string.Join<InputDevice>(",",players[i].devices.ToArray())); Check(players[i].devices.Count==1 && players[i].devices[0]==manager.slots[i].device,"Exclusive pairing "+i);
   Check(players[i].actions.FindAction("Player/Attack").IsPressed()==(i==0),"Fire isolation "+i);
  }
  Seed(manager.slots[0],512,512,false); Seed(manager.slots[1],0,512,true); Seed(manager.slots[2],512,1023,false); Seed(manager.slots[3],512,0,true); TickManager(manager);
  var expected=new[]{Vector2.zero,Vector2.left,Vector2.up,Vector2.down};
  for(int i=0;i<4;i++) Check(Vector2.Distance(players[i].actions.FindAction("Player/Move").ReadValue<Vector2>(),expected[i])<0.001f,"Independent direction "+i);
  Seed(manager.slots[1],0,512,true,button2:true); Seed(manager.slots[2],512,1023,false,switchSide:true); TickManager(manager);
  for(int i=0;i<4;i++) {
   Check(players[i].actions.FindAction("Player/Button2").IsPressed()==(i==1),"Second button isolation "+i);
   Check(players[i].actions.FindAction("Player/SwitchSide").IsPressed()==(i==2),"Third button isolation "+i);
  }
  Seed(manager.slots[1],0,512,true,true,true,true); Seed(manager.slots[2],512,1023,false); TickManager(manager);
  Check(!players[1].actions.FindAction("Player/Button2").IsPressed() && !players[1].actions.FindAction("Player/SwitchSide").IsPressed(),"Stale serial releases extra buttons");
  Check(!players[2].actions.FindAction("Player/SwitchSide").IsPressed(),"Third button releases independently");
  Check(players[1].actions.FindAction("Player/Move").ReadValue<Vector2>()==Vector2.zero && !players[1].actions.FindAction("Player/Attack").IsPressed(),"Stale serial releases move/fire");
  Check(players[3].actions.FindAction("Player/Attack").IsPressed(),"Other Arduino continues after timeout");
  manager.slots[3].enabled=false; TickManager(manager); Check(manager.slots[3].device==null,"Disabled slot removes device");
  manager.slots[3].enabled=true; manager.slots[3].portName="SIM0"; TickManager(manager); Check(manager.slots[3].status=="Duplicate COM port" && manager.slots[3].device==null,"Duplicate ports rejected");
  manager.slots[3].portName="SIM3"; manager.slots[3].player=players[0]; TickManager(manager); Check(manager.slots[3].status=="Player already assigned to another slot","Duplicate player rejected");
  manager.slots[3].player=players[3]; Seed(manager.slots[3],1023,512,false); TickManager(manager); Check(manager.slots[3].device!=null,"Reassignment recovers");
  manager.slots[0].enabled=false; TickManager(manager); Check(players[0].currentControlScheme=="Keyboard&Mouse","Desktop scheme restored");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.Space)); Pump();
  Check(players[0].actions.FindAction("Player/Move").ReadValue<Vector2>()==Vector2.up && players[0].actions.FindAction("Player/Attack").IsPressed(),"Keyboard movement/fire still works");
  var c=new ardunoManager.ArduinoSlot(); Check(ardunoManager.Calibrate(512,512,c)==Vector2.zero,"Center dead zone"); c.invertX=true;
  Check(ardunoManager.Calibrate(1023,512,c)==Vector2.left,"Per-device inversion"); c.swapAxes=true;
  Check(ardunoManager.Calibrate(1023,512,c)==Vector2.up,"Per-device axis swap");
  int x,y; bool fire; Check(ArduinoSerialConnection.TryParse("512,512,1\r",out x,out y,out fire)&&fire,"Fire protocol");
  Check(ArduinoSerialConnection.TryParse("512,512",out x,out y,out fire)&&!fire,"Old protocol");
  Check(!ArduinoSerialConnection.TryParse("512,512,9",out x,out y,out fire),"Invalid fire rejected");
  bool second,third;
  for(int mask=0;mask<8;mask++) {
   Check(ArduinoSerialConnection.TryParse("504,512,"+(mask&1)+","+((mask>>1)&1)+","+((mask>>2)&1)+"\r",out x,out y,out fire,out second,out third)
    && fire==((mask&1)!=0) && second==((mask&2)!=0) && third==((mask&4)!=0),"Three-button protocol combination "+mask);
  }
  Check(ArduinoSerialConnection.TryParse("504,512,0,1",out x,out y,out fire,out second,out third)&&second&&!third,"Four-field compatibility");
  Check(ArduinoSerialConnection.TryParse("504,512,1",out x,out y,out fire,out second,out third)&&fire&&!second&&!third,"Missing buttons released");
  foreach(var bad in new[]{"512,512,0,2,0","512,512,0,0,2","512,512,0,0,","512,512,0,0,0,0"})
   Check(!ArduinoSerialConnection.TryParse(bad,out x,out y,out fire,out second,out third),"Reject invalid packet "+bad);
  manager.enabled=false; Pump();
  foreach(var slot in manager.slots) Check(slot.device==null,"Shutdown removes devices");
  UnityEngine.Object.Destroy(managerObject);
  foreach(var pi in players) UnityEngine.Object.Destroy(pi.gameObject);
 }
}





