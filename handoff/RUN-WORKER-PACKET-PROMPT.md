# RUN WORKER PACKET PROMPT

> Story is the source-of-truth contract. Packet is local execution guidance.

## AUTONOMOUS — DEFAULT

Run the packets in story order without human approval between packets.

After every packet:

- run the packet verification;
- run relevant lint/checks;
- inspect the scoped diff;
- update implementation-notes.html.

If verification passes and Semantic deviations = NONE, continue automatically to the next packet.

Stop only when:

- verification fails and cannot be fixed locally;
- semantic deviation is not NONE;
- there is a contract conflict;
- project architecture or story scope must change;
- existing semantics must be dropped or changed;
- unexpected runtime/gameplay changes appear.

Do not scan the whole repository, re-plan the story, redesign architecture, or request human approval between autonomous packets.

## SINGLE_PACKET

Use only when the developer/frontier model explicitly requests one packet for debugging or a high-risk task. Execute that packet, verify it, inspect its diff, and report before stopping.
