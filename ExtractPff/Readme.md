
# PFF File Extractor

This tool allows you to extract files from a PFF (archive) file based on a provided manifest file.

## Usage

1. Obtain the PFF file that you want to extract files from.
2. Generate the manifest file using `pack.exe`. This is a utility included in the root of the tachyon installation directory.
3. Use the `extract_pff.py` script to extract the files.

### Generating the Manifest File

The manifest file contains information about the files stored in the PFF archive. You can use `pack.exe` (included in the Tachyon installation files) to generate the manifest file.

To generate the manifest file, open a command prompt and navigate to the directory where `pack.exe` is located. Then run the following command:

```shell
pack.exe <pff_file> /DUMP 
```

Replace `<pff_file>` with the path to your PFF file. This command will create a `update.dmp` file that contains the manifest information.

### Extracting Files

To extract files from the PFF archive, you'll need the `extract_pff.py` script and the manifest file generated in the previous step.

1. Make sure you have Python installed on your system.
2. Place the `extract_pff.py` script in the same directory as the PFF file and the manifest file.
3. Open a command prompt and navigate to the directory containing the script and files.
4. Run the following command:

```shell
python extract_pff.py <pff_file> update.dmp
```

Replace `<pff_file>` with the path to your PFF file. This command will extract the files listed in the manifest and place them in the appropriate directories based on their file extensions.

## Notes

- The manifest file should follow a specific format where each line represents an entry with the format `ENTRY=filename,offset,length`.
- This tool assumes that `pack.exe` is available in the same directory or in a directory included in the system's PATH environment variable.
