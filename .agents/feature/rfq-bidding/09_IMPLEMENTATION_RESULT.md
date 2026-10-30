# RFQ & Bidding — Implementation Result

## Delivered scope

- Customer RFQ lifecycle: create a draft, publish, list owned RFQs, cancel before award, and automatic deadline closure.
- Supplier matching: only verified suppliers with an active shop and reputation of at least 80 are suggested; an RFQ accepts at most 10 invitations.
- Sealed bidding: invited suppliers can view an RFQ, decline it, submit every line item, or update their own submitted bid while the RFQ remains open. Competitor prices are not exposed to suppliers.
- Customer comparison and award: the owner can compare submitted bids side by side and accept exactly one bid. Awarding uses a transaction and row lock; it creates a draft contract and cancels non-winning bids.
- Contract deposit: the customer can send the contract for signature, sign with OTP, then pay the M1 deposit through VNPay. A valid VNPay IPN marks the payment and deposit milestone paid and activates the contract.

## Added or completed API surface

| Method | Endpoint | Role |
| --- | --- | --- |
| GET | `/api/v1/rfqs/supplier` | Supplier |
| GET | `/api/v1/rfqs/{rfqId}/suggested-suppliers` | Customer |
| POST | `/api/v1/payments/vnpay/create-url` | Customer |
| GET | `/api/v1/payments/vnpay/ipn` | VNPay callback |

The pre-existing RFQ create, publish, invitations, bids, comparison, cancellation, and award endpoints are connected to the frontend and retained.

## Frontend integration

- `BiddingAndQuoteComparison.jsx`: real RFQ list, create/publish form, verified-supplier invitation modal, bid comparison, and award action.
- `BiddingPage.jsx`: real supplier invitations, RFQ detail, per-line sealed bid form, and decline action.
- `ContractsAndMilestone.jsx`: real contract/payment data, OTP signature, deposit M1 modal, and secure redirect to the returned VNPay URL.

## Required local configuration

Copy the `VnPay` section from `appsettings.sample.json` into the ignored Web API `appsettings.json` and set the sandbox terminal code and hash secret. Keep both values out of Git. Configure VNPay IPN to:

`{api-base-url}/api/v1/payments/vnpay/ipn`

The browser return URL is supplied by the frontend when it creates a payment URL. Payment state is authoritative only after a valid IPN signature.

## Verification completed

- `dotnet build Mooncake.EcommercePlatform.sln --no-restore`
- `dotnet test ... --filter FullyQualifiedName~VnPayGatewayTests`
- targeted ESLint for RFQ, payment, and contract modules
- `npm run build`

No EF migration is required: this implementation uses the existing RFQ, contract, milestone, payment, and invitation schema.
