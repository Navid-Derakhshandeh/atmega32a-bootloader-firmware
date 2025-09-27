# atmega32a bootloader firmware

I have developed a bootloader for the ATmega32A that works without changing the fuse bits and without requiring an external oscillator. You can upload this bootloader to the ATmega32A using a programmer. After that, connect a USB-to-Serial module to the microcontroller and enter your code into the IDE (which I developed using C#). When you click the "Compile" button, the IDE generates a BIN file. Clicking the "Send" button transmits the BIN file to the ATmega32A via UART, and the microcontroller begins executing the program you wrote.

I also implemented a simple iterator function within the microcontroller, along with basic pull-up and pull-down instructions. As a demonstration, I ran a simple program that blinks an LED (connected to PORTB0) three times.

<img width="492" height="288" alt="Screenshot (4)" src="https://github.com/user-attachments/assets/fe6ca481-ae61-4534-ab75-e738728668f3" />

![1000003884](https://github.com/user-attachments/assets/0b380fa8-5e3d-4a0f-84e3-697fd8bde80f)
