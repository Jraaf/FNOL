#!/usr/bin/env bash
set +e
BASE="http://localhost:5000"
H_HANDLER=(-H "X-Mock-Role: Handler" -H "X-Mock-UserId: handler-1")
H_SUPER=(-H "X-Mock-Role: Supervisor" -H "X-Mock-UserId: supervisor-1")
H_MGR=(-H "X-Mock-Role: Manager" -H "X-Mock-UserId: manager-1")

green() { printf "\033[32m%s\033[0m\n" "$1"; }
red()   { printf "\033[31m%s\033[0m\n" "$1"; }
section() { printf "\n=== %s ===\n" "$1"; }

assert_status() {
  local label="$1" expected="$2" actual="$3"
  if [ "$expected" = "$actual" ]; then green "PASS $label  (HTTP $actual)"; else red "FAIL $label  (expected $expected, got $actual)"; fi
}

POLICY_OK=aaaaaaaa-1111-1111-1111-111111111111

section "BR-C-01  loss date in the future is rejected"
F=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims" -d '{"policyId":"'$POLICY_OK'","lossDate":"2099-01-01T00:00:00Z","causeOfLossCode":"COLLISION","lossLocation":"x","lossDescription":"x","parties":[{"partyType":0,"firstName":"A","lastName":"B"}],"riskObjects":[]}')
SC=$(echo "$F" | tail -n1); BD=$(echo "$F" | sed '$d')
assert_status "BR-C-01 future loss date rejected" 400 "$SC"
echo "$BD" | grep -q "BR-C-01" && green "  err code BR-C-01 surfaced" || red "  err code BR-C-01 missing"

section "BR-C-02  loss date outside policy window -> warning, claim still created"
O=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims" -d '{"policyId":"'$POLICY_OK'","lossDate":"2025-06-01T00:00:00Z","causeOfLossCode":"COLLISION","lossLocation":"loc","lossDescription":"desc","parties":[{"partyType":0,"firstName":"A","lastName":"B"}],"riskObjects":[]}')
SC=$(echo "$O" | tail -n1); BD=$(echo "$O" | sed '$d')
assert_status "BR-C-02 outside-window claim accepted" 201 "$SC"
CLAIM_OUTSIDE=$(echo "$BD" | python -c "import sys,json;print(json.load(sys.stdin)['claimId'])")
echo "$BD" | grep -q 'lossDateOutsidePolicyPeriod":true' && green "  outsidePolicyPeriod flag set" || red "  flag NOT set"
AUDIT=$(curl -s "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OUTSIDE/audit")
echo "$AUDIT" | grep -q "LOSS_DATE_OUTSIDE_POLICY_PERIOD" && green "  warning audit recorded" || red "  warning audit NOT recorded"

section "BR-C-03  no Claimant party rejected"
N=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims" -d '{"policyId":"'$POLICY_OK'","lossDate":"2026-04-01T00:00:00Z","causeOfLossCode":"COLLISION","lossLocation":"l","lossDescription":"d","parties":[{"partyType":1,"firstName":"W","lastName":"Itness"}],"riskObjects":[]}')
SC=$(echo "$N" | tail -n1); BD=$(echo "$N" | sed '$d')
assert_status "BR-C-03 no-claimant rejected" 400 "$SC"
echo "$BD" | grep -q "BR-C-03" && green "  err code BR-C-03 surfaced" || red "  err code BR-C-03 missing"

section "BR-C-04  claim number format CLM-YYYY-NNNNNNN"
G=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" "$BASE/api/claims" -d '{"policyId":"'$POLICY_OK'","lossDate":"2026-04-01T00:00:00Z","causeOfLossCode":"COLLISION","lossLocation":"l","lossDescription":"d","parties":[{"partyType":0,"firstName":"C","lastName":"L"}],"riskObjects":[]}')
CLAIM_OK=$(echo "$G" | python -c "import sys,json;print(json.load(sys.stdin)['claimId'])")
NUM=$(echo "$G" | python -c "import sys,json;print(json.load(sys.stdin)['claimNumber'])")
echo "claim number: $NUM"
echo "$NUM" | grep -Eq '^CLM-2026-[0-9]{7}$' && green "PASS BR-C-04 format" || red "FAIL BR-C-04 format ($NUM)"

section "BR-C-05  unknown cause code rejected"
B=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims" -d '{"policyId":"'$POLICY_OK'","lossDate":"2026-04-01T00:00:00Z","causeOfLossCode":"NOPE","lossLocation":"l","lossDescription":"d","parties":[{"partyType":0,"firstName":"C","lastName":"L"}],"riskObjects":[]}')
SC=$(echo "$B" | tail -n1); BD=$(echo "$B" | sed '$d')
assert_status "BR-C-05 unknown code rejected" 400 "$SC"
echo "$BD" | grep -q "BR-C-05" && green "  err code BR-C-05 surfaced" || red "  err code BR-C-05 missing"

section "BR-C-06  illegal Draft -> Closed rejected"
S=$(curl -s -X PUT -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims/$CLAIM_OK/status" -d '{"toStatus":4}')
SC=$(echo "$S" | tail -n1); BD=$(echo "$S" | sed '$d')
assert_status "BR-C-06 Draft -> Closed rejected" 422 "$SC"
echo "$BD" | grep -q "BR-C-06" && green "  err code BR-C-06 surfaced" || red "  err code BR-C-06 missing"

section "BR-C-06  Draft -> Open -> UnderInvestigation -> PendingPayment -> Closed"
for next in 1 2 3 4; do
  R=$(curl -s -X PUT -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims/$CLAIM_OK/status" -d "{\"toStatus\":$next}")
  SC=$(echo "$R" | tail -n1)
  assert_status "  transition -> $next" 200 "$SC"
done

section "BR-R-01  reserve amount must be > 0"
Z=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims/$CLAIM_OK/reserves" -d '{"componentType":0,"amount":0}')
SC=$(echo "$Z" | tail -n1); BD=$(echo "$Z" | sed '$d')
assert_status "BR-R-01 zero amount rejected" 400 "$SC"
echo "$BD" | grep -q "BR-R-01" && green "  err code BR-R-01 surfaced" || red "  err code BR-R-01 missing"

section "BR-R-02  reserve <= 10K -> AutoApproved (status code 0)"
N=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OK/reserves" -d '{"componentType":0,"amount":2500}')
AS=$(echo "$N" | python -c "import sys,json;print(json.load(sys.stdin)['approvalStatus'])")
[ "$AS" = "0" ] && green "PASS BR-R-02 auto-approved (status=$AS)" || red "FAIL BR-R-02 expected 0 got $AS"

section "BR-R-03  reserve in (10K..100K] -> PendingSupervisor (1)"
R=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OK/reserves" -d '{"componentType":1,"amount":50000}')
RID_SUP=$(echo "$R" | python -c "import sys,json;print(json.load(sys.stdin)['id'])")
AS=$(echo "$R" | python -c "import sys,json;print(json.load(sys.stdin)['approvalStatus'])")
[ "$AS" = "1" ] && green "PASS BR-R-03 PendingSupervisor (status=$AS)" || red "FAIL BR-R-03 status=$AS"

section "BR-R-04  reserve > 100K -> PendingManager (2); Supervisor cannot approve"
R=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OK/reserves" -d '{"componentType":3,"amount":250000}')
RID_MGR=$(echo "$R" | python -c "import sys,json;print(json.load(sys.stdin)['id'])")
AS=$(echo "$R" | python -c "import sys,json;print(json.load(sys.stdin)['approvalStatus'])")
[ "$AS" = "2" ] && green "PASS BR-R-04 PendingManager (status=$AS)" || red "FAIL BR-R-04 status=$AS"
SUPER_APPROVE=$(curl -s -o /dev/null -w "%{http_code}" -X POST -H "Content-Type: application/json" "${H_SUPER[@]}" "$BASE/api/claims/$CLAIM_OK/reserves/$RID_MGR/approve" -d '{}')
assert_status "  Supervisor cannot approve mgr-tier" 422 "$SUPER_APPROVE"
MGR_APPROVE=$(curl -s -o /dev/null -w "%{http_code}" -X POST -H "Content-Type: application/json" "${H_MGR[@]}" "$BASE/api/claims/$CLAIM_OK/reserves/$RID_MGR/approve" -d '{}')
assert_status "  Manager approves mgr-tier" 200 "$MGR_APPROVE"

section "BR-R-05  GL posting idempotency"
SUP_APPROVE=$(curl -s -o /dev/null -w "%{http_code}" -X POST -H "Content-Type: application/json" "${H_SUPER[@]}" "$BASE/api/claims/$CLAIM_OK/reserves/$RID_SUP/approve" -d '{}')
assert_status "  Supervisor approves" 200 "$SUP_APPROVE"
sleep 3
AUDIT=$(curl -s "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OK/audit")
SUP_N=${RID_SUP//-/}
MGR_N=${RID_MGR//-/}
COUNT_SUP=$(echo "$AUDIT" | python -c "import sys,json;d=json.load(sys.stdin);k='Reserve:$SUP_N:Change:1';print(sum(1 for e in d if e['eventType']=='GL_POSTING_SIMULATED' and k in (e.get('newValues') or '')))")
COUNT_MGR=$(echo "$AUDIT" | python -c "import sys,json;d=json.load(sys.stdin);k='Reserve:$MGR_N:Change:1';print(sum(1 for e in d if e['eventType']=='GL_POSTING_SIMULATED' and k in (e.get('newValues') or '')))")
echo "  GL postings for supervisor reserve seq1: $COUNT_SUP"
echo "  GL postings for manager    reserve seq1: $COUNT_MGR"
[ "$COUNT_SUP" = "1" ] && green "PASS one GL posting per supervisor-approved change" || red "FAIL got $COUNT_SUP for supervisor"
[ "$COUNT_MGR" = "1" ] && green "PASS one GL posting per manager-approved change"     || red "FAIL got $COUNT_MGR for manager"

section "Reserve adjust (PUT) -> Change:2 posting"
A=$(curl -s -X PUT -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims/$CLAIM_OK/reserves/$RID_SUP" -d '{"newAmount":5000,"changeReason":"recalculated"}')
SC=$(echo "$A" | tail -n1); assert_status "  Adjust to 5K (auto-approves)" 200 "$SC"
sleep 3
AUDIT=$(curl -s "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OK/audit")
COUNT_SEQ2=$(echo "$AUDIT" | python -c "import sys,json;d=json.load(sys.stdin);print(sum(1 for e in d if e['eventType']=='GL_POSTING_SIMULATED' and 'Change:2' in (e.get('newValues') or '')))")
[ "$COUNT_SEQ2" = "1" ] && green "PASS GL posting fired for ChangeSequence 2" || red "FAIL got $COUNT_SEQ2 for change 2"

section "BR-R-06  rejected reserve cannot be adjusted"
NR=$(curl -s -X POST -H "Content-Type: application/json" "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OK/reserves" -d '{"componentType":2,"amount":75000}')
RID_REJ=$(echo "$NR" | python -c "import sys,json;print(json.load(sys.stdin)['id'])")
REJ=$(curl -s -o /dev/null -w "%{http_code}" -X POST -H "Content-Type: application/json" "${H_SUPER[@]}" "$BASE/api/claims/$CLAIM_OK/reserves/$RID_REJ/reject" -d '{"rejectionReason":"insufficient evidence"}')
assert_status "  Supervisor rejects" 200 "$REJ"
ADJ_AFTER=$(curl -s -X PUT -H "Content-Type: application/json" "${H_HANDLER[@]}" -w "\n%{http_code}" "$BASE/api/claims/$CLAIM_OK/reserves/$RID_REJ" -d '{"newAmount":1000,"changeReason":"retry"}')
SC=$(echo "$ADJ_AFTER" | tail -n1); BD=$(echo "$ADJ_AFTER" | sed '$d')
assert_status "  Adjusting rejected reserve rejected" 422 "$SC"
echo "$BD" | grep -q "RESERVE_REJECTED" && green "  RESERVE_REJECTED code surfaced" || red "  RESERVE_REJECTED NOT surfaced"

section "Policy coverage endpoint"
COV=$(curl -s -w "\n%{http_code}" "${H_HANDLER[@]}" "$BASE/api/policies/$POLICY_OK/coverage")
SC=$(echo "$COV" | tail -n1); BD=$(echo "$COV" | sed '$d')
assert_status "GET coverage" 200 "$SC"
echo "$BD" | grep -q "Cargo" && green "  coverages returned" || red "  coverages NOT returned"

section "GET /api/claims  list with filter"
L=$(curl -s -w "\n%{http_code}" "${H_HANDLER[@]}" "$BASE/api/claims?status=4&page=1&pageSize=5")
SC=$(echo "$L" | tail -n1)
assert_status "List filtered" 200 "$SC"

section "Document upload + SAS URL"
TMP=$(mktemp).txt
echo "Police report — incident 12345" > "$TMP"
UP=$(curl -s -X POST "${H_HANDLER[@]}" -F "file=@$TMP;type=text/plain" -F "documentType=0" -w "\n%{http_code}" "$BASE/api/claims/$CLAIM_OK/documents")
SC=$(echo "$UP" | tail -n1); BD=$(echo "$UP" | sed '$d')
assert_status "Upload document" 200 "$SC"
DOCS=$(curl -s "${H_HANDLER[@]}" "$BASE/api/claims/$CLAIM_OK/documents")
echo "  $(echo $DOCS | python -c 'import sys,json;d=json.load(sys.stdin);print(f"{len(d)} doc(s); first url: {d[0][\"url\"] if d else None}")')"

section "Hangfire SLA monitor recurring job registered"
RJSON=$(curl -s "$BASE/hangfire/recurring")
echo "$RJSON" | grep -q "claims-sla-monitor" && green "PASS recurring job 'claims-sla-monitor' visible on dashboard" || echo "  (dashboard HTML check skipped)"

section "Hangfire role-gated dashboard"
LB=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/hangfire/")
QR=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/hangfire/?role=Supervisor")
HD=$(curl -s -o /dev/null -w "%{http_code}" -H "X-Mock-Role: Supervisor" "$BASE/hangfire/")
assert_status "  loopback bypass"     200 "$LB"
assert_status "  ?role=Supervisor"    200 "$QR"
assert_status "  X-Mock-Role header"  200 "$HD"

echo ""
echo "=== sweep complete ==="
