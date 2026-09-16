// USB joystick for Unity. Default 10-bit analog readings: 0..1023.
// VRx -> A0, VRy -> A1, SW -> D2, GND -> GND. See Arduino/README.md for power.
void setup() {
  pinMode(2, INPUT_PULLUP); // Optional joystick push button; pressed = LOW.
  Serial.begin(115200);
}

void loop() {
  Serial.print(analogRead(A0));
  Serial.print(',');
  Serial.print(analogRead(A1));
  Serial.print(',');
  Serial.println(digitalRead(2) == LOW ? 1 : 0);
  delay(20); // 50 samples per second
}
