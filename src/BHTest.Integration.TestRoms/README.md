Before building, testroms and firmware need to be placed under `/res` in this project.
Currently, there is no failsafe for missing/omitted test suites, or for missing firmware.
All 5 test suites need to be downloaded from [this repo](https://gitlab.com/tasbot/libre-roms-ci) as pre-compiled archives (the Linux shell scripts do this for you).
Firmware needs to be manually copied into a `fw` dir.
All told, the expected directory structure is:
```
res
├─ BullyGB_artifact
├─ cgb-acid-hell_artifact
├─ cgb-acid2_artifact
├─ dmg-acid2_artifact
├─ fw
│   ├─ GB__World__DMG.bin
│   └─ GBC__World__CGB.bin
├─ Gambatte-testroms_artifact
├─ mealybug-tearoom-tests_artifact
└─ rtc3test_artifact
```

As with EmuHawk, the target framework and configuration for all the BizHawk project deps is dictated by this project. That means .NET Standard 2.0, or .NET 5 if the project supports it.
To build and run the tests in `Release` configuration (or `Debug` if you need that for some reason):
* On Linux, run `run_tests_release.sh` or `run_tests_debug.sh`.
* On Windows, pass `-c Release` to `dotnet test` (must be in this project dir). Omitting `-c` will use `Debug`.

> You can at this point run the tests, but you should probably keep reading to see your options.
>
> Be warned that running every test takes a *really long time*, upwards of 1h30m on my Win10 machine and 2h on my Linux machine.
> And no I don't know why it's slower to run 1/3 of the tests on Linux than all of them on Windows but this has consumed enough of my time already. --yoshi

To run only some suites, comment out applications of the `[DataTestMethod]` attribute in the source. (Or `[TestClass]`.)
You can also disable individual test cases programmatically by modifying `TestUtils.ShouldIgnoreCase` — note that "ignored" here means cases are completely removed, and do not count as "skipped".

By default, known failures are counted as "skipped" *without actually running them*.
Set the env. var `BIZHAWKTEST_RUN_KNOWN_FAILURES=1` to run them as well. They will count as "skipped" if they fail, or "failed" if they succeed unexpectedly.

On Linux, all cases for unavailable cores (Gambatte) are counted as "skipped".

Screenshots may be saved under `/test_output/<suite>` in the repo.
For ease of typing, a random hash is chosen for each case, e.g. `DEADBEEF_*.png`. You'll need to check stdout to match them to the cases' display names. (Windows users, see below for how to enable stdout.)

The env. var `BIZHAWKTEST_SAVE_IMAGES` determines when to save screenshots (usually an expect/actual pair) to disk.
* With `BIZHAWKTEST_SAVE_IMAGES=all`, all screenshots are saved.
* With `BIZHAWKTEST_SAVE_IMAGES=failures` (the default), only screenshots of failed tests are saved.
* With `BIZHAWKTEST_SAVE_IMAGES=none`, screenshots are never saved.

Test results are output using the logger(s) specified on the command-line. (Without the `console` logger, the results are summarised in the console, but prints to stdout are not shown.)
* On Linux, the shell scripts add the `console` and `junit` (to file, for GitLab CI) loggers.
* On Windows, pass `-l "console;verbosity=detailed"` to `dotnet`.

> Note that the results and stdout for each test case are not printed immediately.
> Cases are grouped by test method, and once the set of test cases is finished executing, the outputs are sent to the console all at once.

Linux examples:
```sh
# default: simple regression testing, all test suites, saving failures to disk (should read 3705 passed / 11484 skipped / 0 failed)
./run_tests_release.sh

# every test from every suite, not saving anything to disk (as might be used in CI)
BIZHAWKTEST_RUN_KNOWN_FAILURES=1 BIZHAWKTEST_SAVE_IMAGES=none ./run_tests_release.sh
```

Windows examples:
```pwsh
# So this boils down to "Yoshi couldn't be bothered porting the 30 LOC script to PowerShell".
# If you have WSL, comment-out the dotnet invocation on the last line of .run_tests_with_configuration.sh and run ./run_tests_release.sh to automate the testrom downloads, then switch back to PowerShell to run dotnet test.
# --yoshi

# default: simple regression testing, all test suites, saving failures to disk (should read 13769 passed / 1420 skipped / 0 failed)
dotnet test -c Release -l "console;verbosity=detailed"

# same as Linux CI example
$Env:BIZHAWKTEST_RUN_KNOWN_FAILURES = 1
$Env:BIZHAWKTEST_SAVE_IMAGES = "all"
dotnet test -c Release -l "console;verbosity=detailed"
```
