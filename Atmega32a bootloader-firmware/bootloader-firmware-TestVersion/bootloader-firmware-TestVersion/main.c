#define F_CPU 1000000UL

#include <avr/io.h>
#include <util/delay.h>

#define BAUD 9600
#define MYUBRR F_CPU/16/BAUD-1
#define LOOP_BUFFER_SIZE 64

void uart_init(unsigned int ubrr) {
	UBRRH = (unsigned char)(ubrr>>8);
	UBRRL = (unsigned char)ubrr;
	UCSRB = (1<<RXEN)|(1<<TXEN);
	UCSRC = (1<<URSEL)|(1<<UCSZ1)|(1<<UCSZ0);
}

unsigned char uart_receive(void) {
	while (!(UCSRA & (1<<RXC)));
	return UDR;
}

void led_on() {
	PORTB |= (1 << PB0);
}

void led_off() {
	PORTB &= ~(1 << PB0);
}

void wait_ms(uint16_t ms) {
	while (ms--) _delay_ms(1);
}

int main(void) {
	DDRB |= (1 << PB0); // Set PB0 as output
	uart_init(MYUBRR);

	while (1) {
		uint8_t cmd = uart_receive();

		if (cmd == 0x01) {
			led_on();
			} else if (cmd == 0x02) {
			led_off();
			} else if (cmd == 0x03) {
			uint8_t low = uart_receive();
			uint8_t high = uart_receive();
			uint16_t ms = (high << 8) | low;
			wait_ms(ms);
			} else if (cmd == 0x04) { // loop start
			uint8_t count = uart_receive();
			uint8_t loopBuffer[LOOP_BUFFER_SIZE];
			uint8_t index = 0;

			// Read until 0x05 (loop end)
			while (1) {
				uint8_t loopCmd = uart_receive();
				if (loopCmd == 0x05) break;

				loopBuffer[index++] = loopCmd;

				// If it's a delay, read 2 extra bytes
				if (loopCmd == 0x03 && index + 2 < LOOP_BUFFER_SIZE) {
					loopBuffer[index++] = uart_receive();
					loopBuffer[index++] = uart_receive();
				}

				// Prevent buffer overflow
				if (index >= LOOP_BUFFER_SIZE) break;
			}

			// Execute loop block
			for (uint8_t i = 0; i < count; i++) {
				uint8_t j = 0;
				while (j < index) {
					uint8_t loopCmd = loopBuffer[j++];
					if (loopCmd == 0x01) {
						led_on();
						} else if (loopCmd == 0x02) {
						led_off();
						} else if (loopCmd == 0x03) {
						uint8_t low = loopBuffer[j++];
						uint8_t high = loopBuffer[j++];
						uint16_t ms = (high << 8) | low;
						wait_ms(ms);
					}
				}
			}
		}
	}
}


