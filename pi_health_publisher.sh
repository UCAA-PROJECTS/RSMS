#!/usr/bin/env bash
# =============================================================================
# RSMS - Raspberry Pi infrastructure health publisher
# =============================================================================
# Runs ON each shelter's Raspberry Pi. Every INTERVAL seconds it reads the Pi's
# real system status (CPU, memory, disk, network, MQTT publisher) and publishes
# it as JSON to  shelters/<SHELTER_CODE>/gateway  via mosquitto_pub. The RSMS
# server stores it and the System Health page shows it live.
#
# Requires: mosquitto-clients   (sudo apt install -y mosquitto-clients)
# Run:      SHELTER_CODE=GP001 BROKER=192.168.x.x ./pi_health_publisher.sh
# Service:  install as a systemd unit so it starts on boot (see bottom of file).
# =============================================================================
set -u

SHELTER_CODE="${SHELTER_CODE:-GP001}"
BROKER="${BROKER:-localhost}"
PORT="${PORT:-1883}"
INTERVAL="${INTERVAL:-5}"
TOPIC="shelters/${SHELTER_CODE}/gateway"
FAILED=0

iface="$(ip route 2>/dev/null | awk '/^default/{print $5; exit}')"
[ -z "${iface}" ] && iface="$(awk -F: 'NR>2 && $1 !~ /lo/ {gsub(/ /,"",$1); print $1; exit}' /proc/net/dev)"

# ---- node identity (collected once; rarely changes) ----
clean() { tr -d '"\0' | tr -s ' ' | sed 's/^ *//;s/ *$//'; }
HOSTNAME_V="$(hostname 2>/dev/null | clean)"
PI_MODEL="$(tr -d '\0' < /proc/device-tree/model 2>/dev/null | clean)"; [ -z "${PI_MODEL}" ] && PI_MODEL="$(uname -m)"
IP_ADDR="$(hostname -I 2>/dev/null | awk '{print $1}')"
OS_VER="$( . /etc/os-release 2>/dev/null; printf '%s' "${PRETTY_NAME:-}" | clean )"; [ -z "${OS_VER}" ] && OS_VER="$(uname -s)"
KERNEL_V="$(uname -r 2>/dev/null | clean)"
CORES="$(nproc 2>/dev/null || echo 1)"

bool() { [ "$1" = "1" ] || [ "$1" = "true" ] && echo true || echo false; }

read_net() { # -> "rxbytes rxpkts rxdrop txbytes txpkts txdrop"
  awk -v i="${iface}:" '$1==i{print $2, $3, $5, $10, $11, $13}' /proc/net/dev 2>/dev/null
}

echo "RSMS Pi health -> ${TOPIC} on ${BROKER}:${PORT} every ${INTERVAL}s (iface=${iface:-none})"

while true; do
  ts="$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)"

  # ---- CPU ----
  read load1 load5 load15 _rest < /proc/loadavg 2>/dev/null; cores="${CORES:-1}"
  cpu_load="$(awk -v l="${load1:-0}" -v c="${cores:-1}" 'BEGIN{printf "%.1f",(c>0?l/c*100:0)}')"
  if [ -r /sys/class/thermal/thermal_zone0/temp ]; then
    cpu_temp="$(awk '{printf "%.1f",$1/1000}' /sys/class/thermal/thermal_zone0/temp)"
  else
    cpu_temp="$(vcgencmd measure_temp 2>/dev/null | grep -o '[0-9.]*' | head -1)"; cpu_temp="${cpu_temp:-0}"
  fi
  if command -v vcgencmd >/dev/null 2>&1; then
    clk="$(vcgencmd measure_clock arm 2>/dev/null | sed 's/.*=//')"; clock_mhz="$(awk -v c="${clk:-0}" 'BEGIN{printf "%.0f",c/1000000}')"
  elif [ -r /sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq ]; then
    clock_mhz="$(awk '{printf "%.0f",$1/1000}' /sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq)"
  else clock_mhz=0; fi
  uptime_s="$(awk '{printf "%d",$1}' /proc/uptime 2>/dev/null || echo 0)"
  undervolt=0; throttled=0
  if command -v vcgencmd >/dev/null 2>&1; then
    thr="$(( $(vcgencmd get_throttled 2>/dev/null | sed 's/throttled=//' || echo 0) ))"
    [ $(( thr & 0x1 )) -ne 0 ] && undervolt=1
    { [ $(( thr & (1<<16) )) -ne 0 ] || [ $(( thr & (1<<18) )) -ne 0 ]; } && throttled=1
  fi

  # ---- Memory (MB) ----
  read mem_total mem_used mem_avail <<<"$(free -m 2>/dev/null | awk '/^Mem:/{print $2, $3, $7}')"
  swap_used="$(free -m 2>/dev/null | awk '/^Swap:/{print $3}')"

  # ---- Disk (GB) + inodes (%) ----
  read disk_total disk_used disk_free <<<"$(df -BG / 2>/dev/null | awk 'NR==2{gsub(/G/,"",$2);gsub(/G/,"",$3);gsub(/G/,"",$4);print $2, $3, $4}')"
  inode_pct="$(df -i / 2>/dev/null | awk 'NR==2{gsub(/%/,"",$5);print $5}')"

  # ---- Network (throughput over the interval; cumulative packets/drops) ----
  s1="$(read_net)"; sleep "${INTERVAL}"; s2="$(read_net)"
  read rxb1 rxp1 rxd1 txb1 txp1 txd1 <<<"${s1:-0 0 0 0 0 0}"
  read rxb2 rxp2 rxd2 txb2 txp2 txd2 <<<"${s2:-0 0 0 0 0 0}"
  thr_kbps="$(awk -v a="${rxb1:-0}" -v b="${rxb2:-0}" -v c="${txb1:-0}" -v d="${txb2:-0}" -v t="${INTERVAL}" 'BEGIN{printf "%.0f", (((b-a)+(d-c))/(t>0?t:1))/1024}')"
  pkts_sent="${txp2:-0}"; pkts_recv="${rxp2:-0}"; pkts_lost="$(( ${rxd2:-0} + ${txd2:-0} ))"
  net_up=0; [ -r "/sys/class/net/${iface}/operstate" ] && [ "$(cat /sys/class/net/${iface}/operstate)" = "up" ] && net_up=1

  # ---- MQTT publisher / clock ----
  clock_synced=0
  if command -v timedatectl >/dev/null 2>&1; then
    [ "$(timedatectl show -p NTPSynchronized --value 2>/dev/null)" = "yes" ] && clock_synced=1
  else clock_synced=1; fi
  publisher_active=1   # this script is the publisher; it is running

  payload="$(cat <<JSON
{"ShelterCode":"${SHELTER_CODE}","TimeStamp":"${ts}",
"Hostname":"${HOSTNAME_V}","PiModel":"${PI_MODEL}","IpAddress":"${IP_ADDR}","OsVersion":"${OS_VER}","KernelVersion":"${KERNEL_V}","CpuCores":${CORES:-1},
"CpuLoad":${cpu_load:-0},"CpuTemperature":${cpu_temp:-0},"ClockFrequencyMhz":${clock_mhz:-0},
"UnderVoltage":$(bool ${undervolt}),"Throttled":$(bool ${throttled}),"UptimeSeconds":${uptime_s:-0},
"Load1":${load1:-0},"Load5":${load5:-0},"Load15":${load15:-0},
"MemoryTotalMb":${mem_total:-0},"MemoryUsedMb":${mem_used:-0},"MemoryAvailableMb":${mem_avail:-0},"SwapUsedMb":${swap_used:-0},
"DiskTotalGb":${disk_total:-0},"DiskUsedGb":${disk_used:-0},"DiskFreeGb":${disk_free:-0},"InodesUsedPercent":${inode_pct:-0},
"NetThroughputKbps":${thr_kbps:-0},"PacketsSent":${pkts_sent:-0},"PacketsReceived":${pkts_recv:-0},"PacketsLost":${pkts_lost:-0},"NetworkUp":$(bool ${net_up}),
"PublisherServiceActive":$(bool ${publisher_active}),"ClockSynced":$(bool ${clock_synced}),"FailedPublishCount":${FAILED}}
JSON
)"

  if mosquitto_pub -h "${BROKER}" -p "${PORT}" -t "${TOPIC}" -m "${payload}" -q 1 2>/dev/null; then
    echo "$(date +%H:%M:%S) published (${cpu_load}% cpu, ${mem_used:-0}/${mem_total:-0}MB, ${thr_kbps}KB/s)"
  else
    FAILED=$(( FAILED + 1 )); echo "$(date +%H:%M:%S) publish FAILED (total ${FAILED})"
  fi
done

# --- Optional systemd unit: /etc/systemd/system/rsms-health.service -----------
# [Unit]
# Description=RSMS Pi health publisher
# After=network-online.target
# [Service]
# Environment=SHELTER_CODE=GP001
# Environment=BROKER=192.168.x.x
# ExecStart=/home/pi/pi_health_publisher.sh
# Restart=always
# [Install]
# WantedBy=multi-user.target
# Then: sudo systemctl enable --now rsms-health
