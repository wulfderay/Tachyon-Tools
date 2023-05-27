import os
import sys

if len(sys.argv) < 3:
    print("Usage: {} <pff_file> <manifest_file>".format(sys.argv[0]))
    sys.exit(1)

pff_file = sys.argv[1]
manifest_file = sys.argv[2]

if not os.path.isfile(pff_file):
    print("Error: PFF file not found")
    sys.exit(1)

if not os.path.isfile(manifest_file):
    print("Error: Manifest file not found")
    sys.exit(1)

with open(manifest_file, 'r') as f:
    pff_data = open(pff_file, 'rb').read()  # Read the entire PFF file

    header_size = 20  # Size of the header in bytes
    offset = header_size  # Start at the offset right after the header

    for line in f:
        if line.startswith('ENTRY='):
            parts = line.split(',')
            filename = parts[0][6:]
            length = int(parts[2])

            extracted_data = pff_data[offset:offset+length]

            extension = os.path.splitext(filename)[1]  # Extract the file extension
            directory = extension[1:]  # Remove the leading dot from the extension

            os.makedirs(directory, exist_ok=True)  # Create the directory if it doesn't exist

            output_file = os.path.join(directory, filename)  # Build the path to the output file

            with open(output_file, 'wb') as outfile:
                outfile.write(extracted_data)

            print("{} extracted to {}".format(filename, output_file))

            offset += length

print("All files extracted successfully")
