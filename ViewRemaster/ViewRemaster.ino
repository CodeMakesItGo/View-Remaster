/* View-Remaster scanner platform
 * https://youtu.be/6Y3vohi8WqI
 * 
 */
#include <FastLED.h>

#define VERSION "ViewRemaster 1.1\n"
 
//Stepper Motor 
#define HALF_STEP 1         // slowest but highest torque
#define FULL_STEP 2         // medium speed medium torque
//#define DOUBLE_STEP 3     // fast speed low torque
#define STEPPER_DELAY 3     // 3ms cycle period
#define ALIGN_COUNT 300     // Min step count for the alignment indent
#define SLACK       30      // Slack in gear

//Ring LED
#define NUM_LEDS 7
#define LED_PIN 7
#define BRIGHTNESS 64

enum MOTOR_STATE {OFF, FWD, REV};
enum FUNCTION {NONE, NEXT, ALIGN};
enum SLIDE_STATE {START, GAP_START, GAP_END, CENTER, END};

static MOTOR_STATE motor_state = OFF;   // Stepper motor state
static FUNCTION function = NONE;        // The Serial command function
static SLIDE_STATE state = START;       // Slide state when searching for next slide or alignment of wheel

static String inputString = "";      // /The String to hold incoming data
static bool stringComplete = false;  // if string contains '\n'
static int stepCounter = 0;          // The stepper motor counter for centering slides
static int STEP_MODE = HALF_STEP;    // Default

// Define the array of leds
CRGB leds[NUM_LEDS];

/// The stepperOutput will control the stepper motor 
void stepperOutput()
{
  static int output = 0;
 
  if(motor_state == OFF)
  {
    return;
    digitalWrite(PD2, LOW);
    digitalWrite(PD3, LOW);
    digitalWrite(PD4, LOW);
    digitalWrite(PD5, LOW);
  }
  else
  {
    stepCounter++;
    
    //Using Full Steps
    if(motor_state == FWD)
    {
      output += STEP_MODE;
      
      //Limit to range 0 - 7
      output = output % 8;
    }
    else
    {
      output -= STEP_MODE;
      if(output < 0)
      {
        output = 7;
      }
    }

     //enable output based on counter and step type
    digitalWrite(PD2, output == 7 || output == 0 || output == 1 ? HIGH : LOW);
    digitalWrite(PD3, output == 1 || output == 2 || output == 3 ? HIGH : LOW);
    digitalWrite(PD4, output == 3 || output == 4 || output == 5 ? HIGH : LOW);
    digitalWrite(PD5, output == 5 || output == 6 || output == 7 ? HIGH : LOW);
  }
}

void serialInput()
{
  // print the string when a newline arrives:
  if (stringComplete) 
  {
    if (inputString.equalsIgnoreCase("N\n")) 
    {
      Serial.println("NEXT");
      function = NEXT;
    }
    else if (inputString.equalsIgnoreCase("A\n")) 
    {
      STEP_MODE = FULL_STEP;
      motor_state = REV;
      
      int revCount = ALIGN_COUNT * 2;
      while(revCount-- > 0)
      {
        stepperOutput();
        delay(STEPPER_DELAY);
      }
        
      motor_state = OFF;
      function = NONE;
      
      Serial.println("ALIGN");
      function = ALIGN;
    }
    else if (inputString.equalsIgnoreCase("U\n")) 
    {
      Serial.println("UP");
      STEP_MODE = HALF_STEP;
      motor_state = FWD;
      stepperOutput();
      motor_state = OFF;
      function = NONE;
      Serial.println("DONE");
    }
    else if (inputString.equalsIgnoreCase("D\n")) 
    {
      Serial.println("DOWN");
      STEP_MODE = HALF_STEP;
      motor_state = REV;
      stepperOutput();
      motor_state = OFF;
      function = NONE;
      Serial.println("DONE");
    }
    else if (inputString.equalsIgnoreCase("S\n")) 
    {
      Serial.println("STOP");
      function = NONE;
      Serial.println("DONE");
    }           
    else if (inputString.startsWith("B")) 
    {
      int value = inputString.substring(1).toInt();
      if(value >= 0 && value <= 255)
      {
        Serial.println("Brightness = " + String(value));
        FastLED.setBrightness(  value );
        FastLED.show();
      }
      else
      {
        Serial.println("Brightness = invalid");
      }
      Serial.println("DONE");
    }
    if (inputString.startsWith("L")) 
    {
      int value = inputString.substring(1).toInt();
      if(value == 0)
      {
        analogWrite(10, 255);
        analogWrite(11, 255);
        Serial.println("Light = Off");
      }
      else
      {
        analogWrite(10, value & 0x01 != 0 ? 0 : 255);
        analogWrite(11, value >> 1 ? 0 : 255);
        Serial.println("Light = On");
      }
      Serial.println("DONE");
    }
    else if (inputString.startsWith("C")) 
    {
      int s = 1;
      int e = inputString.indexOf(',');
      bool success = false;

      do
      {
        if(e == -1) break;
        int r = inputString.substring(s, e).toInt();
        //Serial.println(r);
        s = e + 1;
        e = inputString.indexOf(',', s);

        if(e == -1) break;
        int g = inputString.substring(s, e).toInt();
        //Serial.println(g);
        s = e + 1;
       
        int b = inputString.substring(s).toInt();
        //Serial.println(b);

        Serial.println("Color = " + String(r) + "," + String(g) + "," + String(b));
        
        for(int i = 0; i < NUM_LEDS; i++)
        {
          leds[i].setRGB(r,g,b);
        }
        FastLED.show();
        success = true;
        
      }while(false);

      if(!success)
      {
        Serial.println("Color = invalid");
      }

      Serial.println("DONE");
    }

    // clear the string:
    inputString = "";
    stringComplete = false;
  }
}

void nextSlide(bool align)
{
  //Counter to center slides
  static int stepReverse = 0;

  //Solid part of wheel
  if(digitalRead(PD6) == HIGH)
  {
    if(state == START)
    {
       stepCounter = 0;
       state = GAP_START;
       motor_state = FWD;
    }
    
    else if(state == GAP_END)
    {
      if(align && stepCounter < (ALIGN_COUNT / STEP_MODE))
      {
        state = START;
        motor_state = FWD;
      }
      else
      {
        motor_state = REV;
        state = CENTER;
        stepReverse = stepCounter / 2;
        stepReverse += SLACK;
        stepCounter = 0;
      }
    }
    else if(state == CENTER)
    {
      motor_state = REV;
    }
  }
    
  //Gap in the wheel
  else if(digitalRead(PD6) == LOW)
  {
    if(state == START) 
    {
      motor_state = FWD;
    }
    else if(state == GAP_START) 
    {
      if(stepCounter < 300)
      {
        state = START;
      }
      else
      {
        stepCounter = 0;
        state = GAP_END;
      }
    }
    else if(state == CENTER)
    {
      if(stepCounter >= stepReverse)
      {
        state = END;
        motor_state = OFF;
        function = NONE;
        Serial.println("DONE");
      }
    }
  }
}

void loop() 
{
  static long last_update = millis();
   
  switch(function)
  {
    case NEXT:
      STEP_MODE = FULL_STEP;
      nextSlide(false);
    break;

    case ALIGN:
      STEP_MODE = FULL_STEP;
      nextSlide(true);
    break;

    default:
      motor_state = OFF;
      state = START;
  }

  if(millis() - last_update >= STEPPER_DELAY)
  {
    last_update = millis();
    stepperOutput();
  }

  serialInput();
}

void setup() 
{
  // Start the Serial comms
  Serial.begin(9600);         
  delay(10);
  Serial.println(VERSION);

  //Start FastLED
  FastLED.addLeds<NEOPIXEL, LED_PIN>(leds, NUM_LEDS);  // GRB ordering is assumed
  FastLED.setBrightness(  BRIGHTNESS );
  
  // reserve 32 bytes for the inputString:
  inputString.reserve(32);

  //Pin Setup to control the Stepper motor
  pinMode(PD2, OUTPUT);
  pinMode(PD3, OUTPUT);
  pinMode(PD4, OUTPUT);
  pinMode(PD5, OUTPUT);
  digitalWrite(PD2, LOW);
  digitalWrite(PD3, LOW);
  digitalWrite(PD4, LOW);
  digitalWrite(PD5, LOW);
  
  //Pin setup to read optical endstops
  pinMode(PD6, INPUT);

  //Pin for the spotlight
  pinMode(10, OUTPUT);
  digitalWrite(10, LOW);
  pinMode(11, OUTPUT);
  digitalWrite(11, LOW);
  
  motor_state = OFF;

  for(int i = 0; i < NUM_LEDS; i++)
  {
    leds[i] = CRGB::White;
  }
  FastLED.show();
}

void serialEvent() 
{
  while (Serial.available()) 
  {
    // get the new byte:
    char inChar = (char)Serial.read();
    
    // add it to the inputString:
    inputString += inChar;
    
    // if the incoming character is a newline, set a flag
    stringComplete = (inChar == '\n');
  }
}
