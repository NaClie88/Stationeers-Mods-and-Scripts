# IC10 Airlock — Scripts

Copy-paste ready. Nothing but the code for each chip's IC10 editor
window in-game. For why any of this looks the way it does — design
rationale, corrections, dry-run verification — see
`ic10_airlock_code_notes.md`. For hardware, wiring, and first-time
setup, see `ic10_airlock_setup_guide.md`.

## Watcher

```
# Watcher chip: Power Tier monitor + Button reader + zone-gate control.
# Always powered - never gated off, unlike the Cycle chip below.
# Owns: dedicated Power Controller, shared Light, Cycle-zone power gate,
# Logic Transmitter (broadcasts live E/I/C button state to Cycle chip).

alias Battery d0
alias Light d1
alias Gate d2
alias Transmitter d3

define BtnHash -1591419276
define BtnEName HASH("AirlockBtnE")
define BtnIName HASH("AirlockBtnI")
define BtnCName HASH("AirlockBtnC")
define WakeHold 20

move r0 0
move r7 0

loop:
l r1 Battery Charge
l r2 Battery Maximum
div r1 r1 r2
mul r1 r1 100

beq r0 0 fromNorm
beq r0 1 fromLow
j fromCrit

fromNorm:
bgt r1 90 stay
move r0 1
j stay

fromLow:
bge r1 93 up
ble r1 10 down
j stay
up:
move r0 0
j stay
down:
move r0 2
j stay

fromCrit:
bgt r1 13 riseCrit
j stay
riseCrit:
move r0 1

stay:
s Light Setting r0

lbn r3 BtnHash BtnEName Activate 0
lbn r4 BtnHash BtnIName Activate 0
lbn r5 BtnHash BtnCName Activate 0
s Transmitter Channel1 r5
s Transmitter Channel2 r3
s Transmitter Channel3 r4

move r6 0
beq r0 0 forceHold
beq r0 2 forceHold
bnez r3 forceHold
bnez r4 forceHold
bnez r5 forceHold
j checkHold
forceHold:
move r6 1
checkHold:
bnez r6 doHold
bgtz r7 stillHeld
s Gate On 0
j endLoop
stillHeld:
sub r7 r7 1
j gateOn
doHold:
move r7 WakeHold
gateOn:
s Gate On 1
endLoop:
yield
j loop
```

## Cycle

```
# Cycle chip: owns Doors, Vent, chamber Gas Sensor. Powered only when
# Watcher's zone gate is on - not running otherwise, no separate Deep
# Idle logic needed here, Watcher already handles that upstream.

alias Light d0
alias DoorExt d1
alias DoorInt d2
alias Vent d3
alias Receiver d4
alias ChamberSensor d5

define PropFlagHash -1234567
define TargetInt 100
define TargetExt 2

move r10 0
move r11 0
move r13 0

loop:
l r0 Light Setting
beq r0 2 tierCrit
beq r0 0 checkProp
j cycleCheck

checkProp:
lb r5 PropFlagHash Setting 0
beqz r5 cycleCheck
s DoorExt Open 1
s DoorInt Open 1
j endLoop

cycleCheck:
bgtz r11 doorTimer
bnez r13 continueCycle
l r14 DoorExt Open
l r15 DoorInt Open
bgtz r14 endLoop
bgtz r15 endLoop
l r6 Receiver Channel2
l r7 Receiver Channel3
bnez r6 reqExt
bnez r7 reqInt
j endLoop

reqExt:
beq r10 0 openExt
move r13 1
j endLoop
openExt:
s DoorExt Open 1
move r11 10
j endLoop

reqInt:
beq r10 1 openInt
move r13 2
j endLoop
openInt:
s DoorInt Open 1
move r11 10
j endLoop

continueCycle:
beq r13 1 evacuate
j pressurize

evacuate:
s Vent Mode 0
s Vent On 1
l r12 ChamberSensor Pressure
bgt r12 TargetExt endLoop
s Vent On 0
move r10 0
move r13 0
s DoorExt Open 1
move r11 10
j endLoop

pressurize:
s Vent Mode 1
s Vent On 1
l r12 ChamberSensor Pressure
blt r12 TargetInt endLoop
s Vent On 0
move r10 1
move r13 0
s DoorInt Open 1
move r11 10
j endLoop

doorTimer:
sub r11 r11 1
bgtz r11 endLoop
s DoorExt Open 0
s DoorInt Open 0
j endLoop

tierCrit:
l r8 Receiver Channel1
bnez r8 endLoop
s DoorExt Open 0
s DoorInt Open 0
s Vent Mode 0
s Vent On 1
l r12 ChamberSensor Pressure
bgt r12 TargetExt endLoop
s Vent On 0
s DoorExt Lock 0
s DoorInt Lock 0

endLoop:
yield
j loop
```

## Gas Sensor / Propped-Open Monitor (optional)

```
# Gas Sensor chip: OPTIONAL. Only build this if you installed both
# Gas Sensors. Broadcasts match/mismatch via a type-hash batch flag
# the Cycle chip reads with its own "lb" call - both chips address by
# type-hash only, no device name/Labeller needed, so they always agree
# on what they're reading/writing.
# If this chip doesn't exist, Cycle's batch reads of the same flag
# simply return nothing - no error, Propped-Open just never triggers.
# No single "Ratio" field exists for composition - check Oxygen
# (breathable) plus Pollutant/Methane/NOx (hazard) per-gas instead.

alias SensExt d0
alias SensInt d1

define PropFlagHash -1234567   # must match the Cycle chip's constant
                                 # exactly - each chip defines its own
                                 # copy, they don't share a symbol table

loop:
l r0 SensExt Pressure
l r1 SensInt Pressure
l r2 SensExt Temperature
l r3 SensInt Temperature

move r6 0             # r6 = match flag, default 0 (no match)
sub r7 r0 r1
abs r7 r7
bgt r7 0.1 noMatch     # pressure tol ~0.1 (Custom Airlock V2)
sub r7 r2 r3
abs r7 r7
bgt r7 0.02 noMatch    # temperature tol ~0.02

l r4 SensExt RatioOxygen
l r5 SensInt RatioOxygen
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch   # trace-gas tol ~0.005

l r4 SensExt RatioPollutant
l r5 SensInt RatioPollutant
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch

l r4 SensExt RatioMethane
l r5 SensInt RatioMethane
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch

l r4 SensExt RatioNitrousOxide
l r5 SensInt RatioNitrousOxide
sub r7 r4 r5
abs r7 r7
bgt r7 0.005 noMatch
move r6 1

noMatch:
sb PropFlagHash Setting r6
yield
j loop
```
