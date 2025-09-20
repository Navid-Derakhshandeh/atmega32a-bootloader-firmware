using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Atmega32a_bootloader_firmware_IDE
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            cmbPort.Items.AddRange(SerialPort.GetPortNames());
        }

        private void btnCompile_Click(object sender, EventArgs e)
        {
            string[] lines = txtCode.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<byte> binary = new List<byte>();

            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts[0] == "endloop")
                {
                    ShowCompileError("Unexpected 'endloop' without matching 'loop'");
                    return;
                }

                if (parts[0] == "loop" && parts.Length == 2 && int.TryParse(parts[1], out int count))
                {
                    List<byte> loopBlock = new List<byte>();
                    i++;
                    bool foundEndloop = false;

                    while (i < lines.Length)
                    {
                        string innerLine = lines[i].Trim();
                        if (string.IsNullOrWhiteSpace(innerLine)) { i++; continue; }

                        if (innerLine.Equals("endloop", StringComparison.OrdinalIgnoreCase))
                        {
                            foundEndloop = true;
                            break;
                        }

                        string[] innerParts = innerLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (innerParts.Length == 3 && innerParts[0] == "set" && innerParts[1] == "portb0")
                        {
                            loopBlock.Add(innerParts[2] == "on" ? (byte)0x01 : (byte)0x02);
                        }
                        else if (innerParts.Length == 2 && innerParts[0] == "delay" && ushort.TryParse(innerParts[1], out ushort ms))
                        {
                            loopBlock.Add(0x03);
                            loopBlock.Add((byte)(ms & 0xFF));
                            loopBlock.Add((byte)((ms >> 8) & 0xFF));
                        }
                        else
                        {
                            ShowCompileError($"Invalid line inside loop: {innerLine}");
                            return;
                        }

                        i++;
                    }

                    if (!foundEndloop)
                    {
                        ShowCompileError("Missing 'endloop' for loop block");
                        return;
                    }

                    binary.Add(0x04); // loop start
                    binary.Add((byte)count); // loop count
                    binary.AddRange(loopBlock);
                    binary.Add(0x05); // loop end
                    i++; // move past endloop
                }
                else if (parts.Length == 3 && parts[0] == "set" && parts[1] == "portb0")
                {
                    binary.Add(parts[2] == "on" ? (byte)0x01 : (byte)0x02);
                    i++;
                }
                else if (parts.Length == 2 && parts[0] == "delay" && ushort.TryParse(parts[1], out ushort ms))
                {
                    binary.Add(0x03);
                    binary.Add((byte)(ms & 0xFF));
                    binary.Add((byte)((ms >> 8) & 0xFF));
                    i++;
                }
                else
                {
                    ShowCompileError($"Invalid line: {line}");
                    return;
                }
            }

            File.WriteAllBytes("program.bin", binary.ToArray());
            lblStatus.Text = "Compiled to program.bin";
        }
        private void ShowCompileError(string line)
        {
            MessageBox.Show($"Invalid line: {line}", "Compile Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private void btnUpload_Click(object sender, EventArgs e)
        {
            string port = cmbPort.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(port))
            {
                MessageBox.Show("Select a COM port first.", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists("program.bin"))
            {
                MessageBox.Show("Binary file not found. Compile first.", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes("program.bin");
                using (SerialPort serial = new SerialPort(port, 9600))
                {
                    serial.Open();
                    serial.Write(data, 0, data.Length);
                    serial.Close();
                }
                lblStatus.Text = "Upload complete.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Upload failed: {ex.Message}", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
