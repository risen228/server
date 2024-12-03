namespace Argon.Users;

[TsEnum]
public enum LockdownReason
{
    NONE = 0,
    SPAM_SCAM_ACCOUNT,
    INCITING_MOMENT,
    NON_BINARY_PERSON,
    TOS_VIOLATION,
    LGBT_AGITATION,
    DRUG_VIOLATION,
    TERRORISM_AGITATION,
    CHILD_ABUSE
}