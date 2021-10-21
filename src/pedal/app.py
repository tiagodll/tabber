import time

import board
import digitalio
import usb_hid
from adafruit_hid.keyboard import Keyboard
from adafruit_hid.keyboard_layout_us import KeyboardLayoutUS
from adafruit_hid.keycode import Keycode

keypress_pins = [board.GP13, board.GP14, board.GP15]
key_pin_array = []
keys_pressed = [Keycode.LEFT_ARROW, Keycode.RIGHT_ARROW, Keycode.SPACEBAR]
#control_key = Keycode.SHIFT

time.sleep(1)  # Sleep for a bit to avoid a race condition on some systems
keyboard = Keyboard(usb_hid.devices)
keyboard_layout = KeyboardLayoutUS(keyboard)

# Make all pin objects inputs with pullups
for pin in keypress_pins:
    key_pin = digitalio.DigitalInOut(pin)
    key_pin.direction = digitalio.Direction.INPUT
    key_pin.pull = digitalio.Pull.UP
    key_pin_array.append(key_pin)

led = digitalio.DigitalInOut(board.GP25)
led.direction = digitalio.Direction.OUTPUT

print("Waiting for key pin...")

while True:
    for key_pin in key_pin_array:
        if not key_pin.value:  # Is it grounded?
            i = key_pin_array.index(key_pin)
            print("Pin #{} is grounded.".format(i))

            led.value = True

            while not key_pin.value:
                pass  # Wait for it to be ungrounded, to print on key up

            # "Type" the Keycode or string
            key = keys_pressed[i]  # Get the corresponding Keycode or string
            if isinstance(key, str):  # If it's a string...
                keyboard_layout.write(key)  # ...Print the string
            else:  # If it's not a string...
                #keyboard.press(control_key, key)  # "Press"...
                keyboard.press(key)
                keyboard.release_all()  # ..."Release"!

            led.value = False

    time.sleep(0.01)

