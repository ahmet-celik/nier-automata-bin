# NieR: Automata - Constant Pool Translator

NieR: Automata - Constant Pool Translator for Compiled mruby Binaries (bin files). You can't add new entries to constant pool, however you can translate existing lines without any size limitation.

# Usage

You need .NET Framework 4.7 and Visual Studio on Windows, or MonoDevelop on Linux to build project.
```
 exporting constant pool: nier-automata-bin.exe p300_33eec348_scp.bin
           Each constant will be in a line of p300_33eec348_scp.bin.txt.
           <CR> and <LF> are special strings. Do not delete them.
 importing constant pool: nier-automata-bin.exe p300_33eec348_scp.bin.txt
           New bin with new constant pool will be p300_33eec348_scp.bin.NEW.
```


# Disclaimer

This project was for fun. It is not for enabling illegal activity. All information is obtained via reverse engineering of legally purchased games and information made public on the internet.

Thanks to Rick for [Gibbed.IO](https://github.com/gibbed/Gibbed.IO).

All trademarks, servicemarks, registered trademarks, and registered servicemarks (including NieR: Automata) are the property of their respective owners.
